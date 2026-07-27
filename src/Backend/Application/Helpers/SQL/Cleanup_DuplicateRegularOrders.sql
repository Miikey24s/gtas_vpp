SELECT CreatedByUserId, Year, Month, COUNT(*) AS DuplicateCount
FROM dbo.Requests
WHERE IsDeleted = 0 AND IsAdditionalOrder = 0
GROUP BY CreatedByUserId, Year, Month
HAVING COUNT(*) > 1;

WITH RankedRegularOrders AS
(
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY CreatedByUserId, Year, Month
            ORDER BY Id ASC
        ) AS RowNumber
    FROM dbo.Requests
    WHERE IsDeleted = 0 AND IsAdditionalOrder = 0
)
UPDATE h
SET
    IsDeleted = 1,
    UpdatedAtUtc = SYSUTCDATETIME()
FROM dbo.Requests h
INNER JOIN RankedRegularOrders r ON h.Id = r.Id
WHERE r.RowNumber > 1;

SELECT CreatedByUserId, Year, Month, COUNT(*) AS DuplicateCount
FROM dbo.Requests
WHERE IsDeleted = 0 AND IsAdditionalOrder = 0
GROUP BY CreatedByUserId, Year, Month
HAVING COUNT(*) > 1;
