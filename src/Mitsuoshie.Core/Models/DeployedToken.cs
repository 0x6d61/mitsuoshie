namespace Mitsuoshie.Core.Models;

public record DeployedToken(
    string FilePath,
    HoneyTokenType HoneyType,
    string OriginalHash,
    DateTime DeployedAt
);
