using System;
using System.Data.SQLite;
using System.IO;

class Program
{
    static string DbPath = Path.Combine(Directory.GetCurrentDirectory(), "snapshot.db");

    static void Main()
    {
        CreateAndSeedDatabase();
        QueryDatabase();
    }

    static void CreateAndSeedDatabase()
    {
        if (File.Exists(DbPath))
        {
            File.Delete(DbPath);
        }

        SQLiteConnection.CreateFile(DbPath);

        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();

        // WAL 모드 설정
        using (var pragmaCmd = new SQLiteCommand("PRAGMA journal_mode=WAL;", conn))
        {
            pragmaCmd.ExecuteNonQuery();
        }

        // 테이블 생성 및 더미 데이터 삽입
        using (var cmd = new SQLiteCommand(conn))
        {
            cmd.CommandText = @"
                CREATE TABLE myTable (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );
                INSERT INTO myTable (Name) VALUES ('Alice');
                INSERT INTO myTable (Name) VALUES ('Bob');
                INSERT INTO myTable (Name) VALUES ('Charlie');
            ";
            cmd.ExecuteNonQuery();
        }

        conn.Close();
        Console.WriteLine("✅ snapshot.db created and seeded.");
    }

    static void QueryDatabase()
    {
        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();

        // 트랜잭션 기반 일관성 보장
        using var txn = conn.BeginTransaction();

        using var cmd = new SQLiteCommand("SELECT * FROM myTable;", conn, txn);
        using var reader = cmd.ExecuteReader();

        Console.WriteLine("📄 myTable contents:");
        while (reader.Read())
        {
            Console.WriteLine($"  ID: {reader["Id"]}, Name: {reader["Name"]}");
        }

        txn.Commit();
        conn.Close();
    }
}