"""Document processor for RAG ingestion.

The processor is intentionally conservative: it can only read files that
live inside the configured document root, it refuses symlinks, it enforces
a hard size cap, and it rejects unsupported extensions. Callers are NOT
permitted to point the processor at arbitrary filesystem paths.
"""

from __future__ import annotations

from typing import List, Dict, Any, Optional
from pathlib import Path
import logging
import os

from .config import settings

logger = logging.getLogger(__name__)


class DocumentSecurityError(PermissionError):
    """Raised when a document request fails a security check."""


class DocumentProcessor:
    """Process documents for RAG ingestion."""

    def __init__(self, root: Optional[Path] = None, max_size: Optional[int] = None,
                 allowed_extensions: Optional[List[str]] = None):
        self._explicit_root = root
        self._max_size = max_size if max_size is not None else settings.max_document_size
        exts = allowed_extensions or list(settings.supported_file_types or [])
        self._allowed_extensions = tuple(ext.lower() for ext in exts)

    def _resolve_root(self) -> Path:
        configured = self._explicit_root or settings.document_root
        if not configured:
            raise DocumentSecurityError(
                "No document root configured. Set the 'document_root' setting "
                "or the DOCUMENT_ROOT environment variable to a directory the "
                "service is allowed to read."
            )
        root = Path(configured).expanduser().resolve(strict=False)
        if not root.exists() or not root.is_dir():
            raise DocumentSecurityError(f"Document root does not exist or is not a directory: {root}")
        return root

    def _validate(self, path: Path, root: Path) -> None:
        if path.is_symlink():
            raise DocumentSecurityError(f"Refusing to read symlink: {path}")
        if not path.exists():
            raise FileNotFoundError(f"File does not exist: {path}")
        if not path.is_file():
            raise DocumentSecurityError(f"Not a regular file: {path}")
        try:
            path.relative_to(root)
        except ValueError:
            raise DocumentSecurityError(
                f"Refusing path outside configured document root: {path}"
            )
        ext = path.suffix.lower()
        if ext not in self._allowed_extensions:
            raise DocumentSecurityError(f"Unsupported file type: {ext}")
        size = path.stat().st_size
        if size > self._max_size:
            raise DocumentSecurityError(
                f"File too large ({size} > {self._max_size} bytes): {path}"
            )

    def process_file(self, file_path: str) -> Dict[str, Any]:
        """Process a file and return extracted text with metadata."""
        if not file_path:
            return {"content": "", "metadata": {"error": "file_path is required"}}

        root = self._resolve_root()
        candidate = Path(file_path).expanduser()
        # If a relative path is given, resolve it under the document root.
        if not candidate.is_absolute():
            candidate = root / candidate
        try:
            resolved = candidate.resolve(strict=False)
        except (OSError, RuntimeError) as exc:
            return {"content": "", "metadata": {"error": f"Invalid path: {exc}"}}

        try:
            self._validate(resolved, root)
        except FileNotFoundError as exc:
            return {"content": "", "metadata": {"error": str(exc)}}
        except DocumentSecurityError as exc:
            return {"content": "", "metadata": {"error": str(exc)}}

        ext = resolved.suffix.lower()
        if ext == ".pdf":
            return self._process_pdf(resolved)
        if ext == ".docx":
            return self._process_docx(resolved)
        if ext in (".txt", ".md"):
            return self._process_text(resolved)
        if ext == ".html":
            return self._process_html(resolved)
        if ext == ".xlsx":
            return self._process_excel(resolved)
        return {"content": "", "metadata": {"error": f"Unsupported file type: {ext}"}}

    def process_text(self, text: str, metadata: Optional[Dict] = None) -> Dict[str, Any]:
        """Process raw text (no filesystem access)."""
        return {"content": text, "metadata": metadata or {}}

    def process_upload(self, filename: str, content: bytes) -> Dict[str, Any]:
        """Persist a freshly uploaded file inside the configured document
        root and return the extracted text.

        The upload is written with a server-generated name to prevent
        path-traversal via ``filename``. The original filename is preserved
        only as metadata.
        """
        root = self._resolve_root()
        ext = Path(filename).suffix.lower()
        if ext not in self._allowed_extensions:
            raise DocumentSecurityError(f"Unsupported file type: {ext}")
        if len(content) > self._max_size:
            raise DocumentSecurityError(
                f"Upload too large ({len(content)} > {self._max_size} bytes)"
            )
        uploads_dir = (settings.upload_dir and Path(settings.upload_dir).expanduser().resolve(strict=False)) or (root / "uploads")
        uploads_dir.mkdir(parents=True, exist_ok=True)
        safe_name = f"upload-{os.urandom(16).hex()}{ext}"
        target = (uploads_dir / safe_name).resolve(strict=False)
        try:
            target.relative_to(root)
        except ValueError:
            raise DocumentSecurityError("Upload would escape document root")
        target.write_bytes(content)
        result = self.process_file(str(target))
        result.setdefault("metadata", {})["original_filename"] = filename
        return result

    def chunk_text(self, text: str, chunk_size: int = 512, overlap: int = 50) -> List[str]:
        """Split text into overlapping chunks."""
        if len(text) <= chunk_size:
            return [text]
        chunks: List[str] = []
        start = 0
        while start < len(text):
            end = min(start + chunk_size, len(text))
            if end < len(text):
                for sep in ["\n\n", "\n", ". ", "! ", "? "]:
                    pos = text.rfind(sep, start, end)
                    if pos != -1:
                        end = pos + 1
                        break
            chunks.append(text[start:end].strip())
            start = max(start + 1, end - overlap)
        return [c for c in chunks if c]

    def _process_pdf(self, path: Path) -> Dict[str, Any]:
        try:
            import pypdf
            reader = pypdf.PdfReader(str(path))
            text = "\n\n".join(page.extract_text() or "" for page in reader.pages)
            return {"content": text, "metadata": {"source": str(path), "pages": len(reader.pages), "type": "pdf"}}
        except Exception as e:
            logger.error(f"PDF processing failed: {e}")
            return {"content": "", "metadata": {"error": str(e)}}

    def _process_docx(self, path: Path) -> Dict[str, Any]:
        try:
            from docx import Document
            doc = Document(str(path))
            text = "\n\n".join(p.text for p in doc.paragraphs if p.text.strip())
            return {"content": text, "metadata": {"source": str(path), "paragraphs": len(doc.paragraphs), "type": "docx"}}
        except Exception as e:
            logger.error(f"DOCX processing failed: {e}")
            return {"content": "", "metadata": {"error": str(e)}}

    def _process_text(self, path: Path) -> Dict[str, Any]:
        try:
            text = path.read_text(encoding="utf-8")
            return {"content": text, "metadata": {"source": str(path), "type": path.suffix[1:]}}
        except Exception as e:
            logger.error(f"Text processing failed: {e}")
            return {"content": "", "metadata": {"error": str(e)}}

    def _process_html(self, path: Path) -> Dict[str, Any]:
        try:
            from bs4 import BeautifulSoup
            html = path.read_text(encoding="utf-8")
            soup = BeautifulSoup(html, "lxml")
            text = soup.get_text(separator="\n", strip=True)
            return {"content": text, "metadata": {"source": str(path), "type": "html"}}
        except Exception as e:
            logger.error(f"HTML processing failed: {e}")
            return {"content": "", "metadata": {"error": str(e)}}

    def _process_excel(self, path: Path) -> Dict[str, Any]:
        try:
            from openpyxl import load_workbook
            wb = load_workbook(str(path), read_only=True)
            text_parts = []
            for sheet in wb.sheetnames:
                ws = wb[sheet]
                for row in ws.iter_rows(values_only=True):
                    row_text = " | ".join(str(cell) for cell in row if cell is not None)
                    if row_text.strip():
                        text_parts.append(row_text)
            return {"content": "\n".join(text_parts), "metadata": {"source": str(path), "sheets": wb.sheetnames, "type": "xlsx"}}
        except Exception as e:
            logger.error(f"Excel processing failed: {e}")
            return {"content": "", "metadata": {"error": str(e)}}


processor = DocumentProcessor()
