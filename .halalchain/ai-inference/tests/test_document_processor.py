"""Tests for the document processor's security guards.

Covers:
- traversal outside the configured document root
- absolute paths that escape the root
- symlinks (refused)
- non-existent files
- directories (refused)
- unsupported extensions
- oversized files
- happy path: a valid file inside the root is processed
"""

import os
import pytest
from pathlib import Path

from src.document_processor import DocumentProcessor, DocumentSecurityError


@pytest.fixture
def root(tmp_path):
    docs = tmp_path / "docs"
    docs.mkdir()
    (docs / "valid.txt").write_text("hello world", encoding="utf-8")
    (docs / "sub").mkdir()
    (docs / "sub" / "nested.md").write_text("# nested", encoding="utf-8")
    return docs


@pytest.fixture
def processor(root):
    return DocumentProcessor(root=root, max_size=10_000, allowed_extensions=[".txt", ".md"])


def test_processes_file_under_root(processor, root):
    result = processor.process_file(str(root / "valid.txt"))
    assert result["content"] == "hello world"
    assert result["metadata"]["type"] == "txt"


def test_processes_nested_file_under_root(processor, root):
    result = processor.process_file(str(root / "sub" / "nested.md"))
    assert result["content"] == "# nested"


def test_traversal_outside_root_is_rejected(processor, root, tmp_path):
    secret = tmp_path / "secret.txt"
    secret.write_text("top secret", encoding="utf-8")
    result = processor.process_file(str(secret))
    assert "outside" in result["metadata"]["error"].lower() or "refusing" in result["metadata"]["error"].lower()


def test_absolute_path_outside_root_is_rejected(processor, tmp_path):
    other = tmp_path / "other.txt"
    other.write_text("nope", encoding="utf-8")
    result = processor.process_file(str(other))
    assert "outside" in result["metadata"]["error"].lower() or "refusing" in result["metadata"]["error"].lower()


def test_symlink_is_rejected(processor, root, tmp_path):
    target = tmp_path / "outside.txt"
    target.write_text("data", encoding="utf-8")
    link = root / "link.txt"
    os.symlink(str(target), str(link))
    result = processor.process_file(str(link))
    assert "symlink" in result["metadata"]["error"].lower()


def test_nonexistent_file_is_reported(processor, root):
    result = processor.process_file(str(root / "missing.txt"))
    assert "does not exist" in result["metadata"]["error"].lower() or "not found" in result["metadata"]["error"].lower()


def test_directory_is_rejected(processor, root):
    result = processor.process_file(str(root / "sub"))
    assert "regular file" in result["metadata"]["error"].lower() or "not" in result["metadata"]["error"].lower()


def test_unsupported_extension_is_rejected(processor, root):
    exe = root / "evil.exe"
    exe.write_text("MZ", encoding="utf-8")
    result = processor.process_file(str(exe))
    assert "unsupported" in result["metadata"]["error"].lower()


def test_oversized_file_is_rejected(processor, root):
    big = root / "big.txt"
    big.write_text("a" * 20_000, encoding="utf-8")
    result = processor.process_file(str(big))
    assert "too large" in result["metadata"]["error"].lower() or "size" in result["metadata"]["error"].lower()


def test_upload_sanitises_filename_and_writes_under_root(root, tmp_path):
    p = DocumentProcessor(root=root, max_size=10_000, allowed_extensions=[".txt"])
    res = p.process_upload("evil.txt", b"sensitive content")
    # The original filename is preserved as metadata; the on-disk file
    # is server-named and located under the root.
    assert res["metadata"]["original_filename"] == "evil.txt"
    # The file must be inside the root and the size must be capped.
    found = list(root.rglob("upload-*.txt"))
    assert len(found) == 1
    assert found[0].read_bytes() == b"sensitive content"


def test_upload_rejects_oversize(root):
    p = DocumentProcessor(root=root, max_size=10, allowed_extensions=[".txt"])
    with pytest.raises(DocumentSecurityError):
        p.process_upload("huge.txt", b"x" * 100)
