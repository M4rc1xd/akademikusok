using MySqlConnector;

const string connectionString =
    "Server=localhost;Port=3306;Database=akademikusok;User ID=root;Password=admin;";

const string currentlyLivingQuery = """
    SELECT nev
    FROM tag
    WHERE elhunyt IS NULL
    ORDER BY nev;
    """;

const string nonHungarianMembersQuery = """
    SELECT tag.nev, tag.identitas, tagsag.tipus, tagsag.ev
    FROM tag
    INNER JOIN tagsag ON tagsag.tagid = tag.id
    WHERE tagsag.tipus IN ('r', 'l')
      AND tag.identitas IS NOT NULL
      AND tag.identitas <> 'magyar'
    ORDER BY tagsag.ev;
    """;

const string firstMembershipQuery = """
    SELECT tag.nev, MIN(tagsag.ev) AS elso_ev
    FROM tag
    INNER JOIN tagsag ON tagsag.tagid = tag.id
    GROUP BY tag.id, tag.nev
    ORDER BY tag.nev;
    """;

const string femaleRatioQuery = """
    SELECT AVG(nem = 'n') AS nok_aranya
    FROM tag;
    """;

const string correspondentToOrdinaryAverageQuery = """
    SELECT AVG(rendes.ev - levelezo.ev) AS atlagos_evek
    FROM (
        SELECT tagid, MIN(ev) AS ev
        FROM tagsag
        WHERE tipus = 'l'
        GROUP BY tagid
    ) AS levelezo
    INNER JOIN (
        SELECT tagid, MIN(ev) AS ev
        FROM tagsag
        WHERE tipus = 'r'
        GROUP BY tagid
    ) AS rendes ON rendes.tagid = levelezo.tagid
    WHERE levelezo.ev < rendes.ev;
    """;

const string tellerQuery = """
    SELECT tag.nev, tagsag.ev, tag.elhunyt
    FROM tag
    INNER JOIN tagsag ON tagsag.tagid = tag.id
    WHERE tagsag.ev <= (
        SELECT teller_tagsag.ev
        FROM tag AS teller
        INNER JOIN tagsag AS teller_tagsag
            ON teller_tagsag.tagid = teller.id
        WHERE teller.nev = 'Teller Ede'
          AND teller_tagsag.tipus = 't'
    )
      AND (
          tag.elhunyt >= (
              SELECT teller.elhunyt
              FROM tag AS teller
              WHERE teller.nev = 'Teller Ede'
          )
          OR tag.elhunyt IS NULL
      )
      AND tagsag.tipus = 't'
    ORDER BY tagsag.ev, tag.nev;
    """;

const string twentiethCenturyOrdinaryMembersQuery = """
    SELECT tagsag.ev, tag.nev
    FROM tag
    INNER JOIN tagsag ON tagsag.tagid = tag.id
    WHERE tagsag.tipus = 'r'
      AND tagsag.ev BETWEEN 1901 AND 2000
    ORDER BY tagsag.ev, tag.nev;
    """;

try
{
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    await PrintTableAsync(connection, "2jelenleg - Élő akadémikusok", currentlyLivingQuery);
    await PrintTableAsync(connection, "3mas - Nem csak magyar identitású rendes vagy levelező tagok",
        nonHungarianMembersQuery);
    await PrintTableAsync(connection, "4mikor - Első tagság éve", firstMembershipQuery);
    await PrintScalarAsync(connection, "5arany - Nők aránya", femaleRatioQuery, "Nők aránya");
    await PrintScalarAsync(connection, "6atlag - Levelező tagság átlagos időtartama",
        correspondentToOrdinaryAverageQuery, "Átlagos évek");
    await PrintTableAsync(connection, "7Teller - Teller Ede tagsága alatt végig tiszteleti tagok",
        tellerQuery);
    await PrintTableAsync(connection, "8rendes - XX. századi rendes tagok", twentiethCenturyOrdinaryMembersQuery);
}
catch (MySqlException exception)
{
    Console.Error.WriteLine($"MySQL hiba: {exception.Message}");
    Environment.ExitCode = 1;
}

static async Task PrintTableAsync(MySqlConnection connection, string title, string query)
{
    Console.WriteLine($"\n--- {title} ---");

    await using var command = new MySqlCommand(query, connection);
    await using var reader = await command.ExecuteReaderAsync();

    var columnNames = Enumerable.Range(0, reader.FieldCount)
        .Select(reader.GetName)
        .ToArray();
    Console.WriteLine(string.Join(" | ", columnNames));

    while (await reader.ReadAsync())
    {
        var values = Enumerable.Range(0, reader.FieldCount)
            .Select(index => reader.IsDBNull(index) ? string.Empty : reader.GetValue(index).ToString())
            .ToArray();
        Console.WriteLine(string.Join(" | ", values));
    }
}

static async Task PrintScalarAsync(
    MySqlConnection connection,
    string title,
    string query,
    string label)
{
    Console.WriteLine($"\n--- {title} ---");

    await using var command = new MySqlCommand(query, connection);
    var result = await command.ExecuteScalarAsync();
    Console.WriteLine($"{label}: {result ?? "nincs adat"}");
}
