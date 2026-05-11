SELECT CreateUserId, Y, M, COUNT(*) AS DuplicateCount
FROM VPP01_RequestHeader
WHERE IsDeleted = 0 AND IsAdditionalOrder = 0
GROUP BY CreateUserId, Y, M
HAVING COUNT(*) > 1;

WITH RankedRegularOrders AS
(
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY CreateUserId, Y, M
            ORDER BY Id ASC
        ) AS RowNumber
    FROM VPP01_RequestHeader
    WHERE IsDeleted = 0 AND IsAdditionalOrder = 0
)
UPDATE h
SET
    IsDeleted = 1,
    UpdateDate = SYSUTCDATETIME()
FROM VPP01_RequestHeader h
INNER JOIN RankedRegularOrders r ON h.Id = r.Id
WHERE r.RowNumber > 1;

SELECT CreateUserId, Y, M, COUNT(*) AS DuplicateCount
FROM VPP01_RequestHeader
WHERE IsDeleted = 0 AND IsAdditionalOrder = 0
GROUP BY CreateUserId, Y, M
HAVING COUNT(*) > 1;
