namespace HalalChain.Platform.Api.CodeGen.Models;

public sealed record GeneratedResource(
    string Name,
    string Segment,
    string BasePath,
    string ApiPath,
    string DtoType,
    string? CreateRequestType,
    string? UpdateRequestType,
    bool HasList,
    bool HasCreate,
    bool HasUpdate,
    bool HasDelete,
    bool HasDetail,
    int Order,
    string Icon,
    string Policy,
    string[] Roles);