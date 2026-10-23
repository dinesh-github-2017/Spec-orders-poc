SET NOCOUNT ON;
USE OrdersPoc;
SELECT
    t.name AS [Table],
    c.column_id AS [Ordinal],
    c.name AS [Column],
    ty.name AS [DataType],
    CASE WHEN ty.name IN ('nvarchar','nchar') THEN c.max_length/2 WHEN ty.name IN ('varchar','char') THEN c.max_length ELSE NULL END AS [Length],
    CASE WHEN ty.name IN ('decimal','numeric') THEN c.precision END AS [Precision],
    CASE WHEN ty.name IN ('decimal','numeric') THEN c.scale END AS [Scale],
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS [Nullable],
    CASE WHEN pk.column_id IS NOT NULL THEN 'YES' ELSE '' END AS [PrimaryKey],
    ISNULL(OBJECT_NAME(fkc.referenced_object_id) + '.' + COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id), '') AS [ForeignKey],
    ISNULL(OBJECT_DEFINITION(c.default_object_id), '') AS [Default],
    ISNULL((SELECT STRING_AGG(cc.definition, ' ') FROM sys.check_constraints cc WHERE cc.parent_object_id = t.object_id AND cc.parent_column_id = c.column_id), '') AS [Check],
    ISNULL((SELECT STRING_AGG(i.name, ' ') FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            WHERE ic.object_id = t.object_id AND ic.column_id = c.column_id AND i.is_primary_key = 0), '') AS [Indexes]
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN (SELECT ic.object_id, ic.column_id FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id WHERE i.is_primary_key = 1) pk
    ON pk.object_id = t.object_id AND pk.column_id = c.column_id
LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = t.object_id AND fkc.parent_column_id = c.column_id
ORDER BY t.name, c.column_id;
