using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

public sealed class OfflineDatabaseSqlParameter
{
    public string Name;
    public object Value;

    public OfflineDatabaseSqlParameter(string name, object value)
    {
        Name = name;
        Value = value;
    }
}

public static class OfflineDatabaseSql
{
    public static IDbConnection OpenConnection(string databasePath = null)
    {
        OfflineDatabaseModule module = string.IsNullOrEmpty(databasePath)
            ? new OfflineDatabaseModule()
            : new OfflineDatabaseModule(databasePath);
        OfflineDatabaseOpenResult openResult = module.OpenOrCreate();
        if (!openResult.Success)
        {
            throw new InvalidOperationException(openResult.Message);
        }

        OfflineDatabaseProviderResolution provider = OfflineDatabaseProvider.Resolve();
        if (!provider.Success)
        {
            throw new InvalidOperationException(provider.ErrorMessage);
        }

        string initializationError = OfflineDatabaseProvider.Initialize(provider);
        if (!string.IsNullOrEmpty(initializationError))
        {
            throw new InvalidOperationException(initializationError);
        }

        IDbConnection connection = OfflineDatabaseProvider.CreateConnection(provider, openResult.DatabasePath);
        if (connection == null)
        {
            throw new InvalidOperationException("Could not create SQLite connection instance.");
        }

        connection.Open();
        ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
        return connection;
    }

    public static int ExecuteNonQuery(
        IDbConnection connection,
        string sql,
        IDbTransaction transaction = null,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        using (IDbCommand command = CreateCommand(connection, sql, transaction, parameters))
        {
            return command.ExecuteNonQuery();
        }
    }

    public static object ExecuteScalar(
        IDbConnection connection,
        string sql,
        IDbTransaction transaction = null,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        using (IDbCommand command = CreateCommand(connection, sql, transaction, parameters))
        {
            return command.ExecuteScalar();
        }
    }

    public static List<T> Query<T>(
        IDbConnection connection,
        string sql,
        Func<IDataRecord, T> map,
        IDbTransaction transaction = null,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        List<T> result = new List<T>();
        using (IDbCommand command = CreateCommand(connection, sql, transaction, parameters))
        using (IDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                result.Add(map(reader));
            }
        }

        return result;
    }

    public static long ReadLastInsertRowId(IDbConnection connection, IDbTransaction transaction = null)
    {
        object result = ExecuteScalar(connection, "SELECT last_insert_rowid();", transaction);
        return result == null || result == DBNull.Value
            ? 0L
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    public static int ReadInt(object value, int fallback = 0)
    {
        if (value == null || value == DBNull.Value)
        {
            return fallback;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public static long ReadLong(object value, long fallback = 0L)
    {
        if (value == null || value == DBNull.Value)
        {
            return fallback;
        }

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    public static float ReadFloat(object value, float fallback = 0f)
    {
        if (value == null || value == DBNull.Value)
        {
            return fallback;
        }

        return Convert.ToSingle(value, CultureInfo.InvariantCulture);
    }

    public static string ReadText(object value, string fallback = "")
    {
        return value == null || value == DBNull.Value ? fallback : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    public static bool ReadBool(object value, bool fallback = false)
    {
        if (value == null || value == DBNull.Value)
        {
            return fallback;
        }

        return ReadInt(value, fallback ? 1 : 0) != 0;
    }

    public static string UtcNowText()
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    private static IDbCommand CreateCommand(
        IDbConnection connection,
        string sql,
        IDbTransaction transaction,
        params OfflineDatabaseSqlParameter[] parameters)
    {
        IDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        if (transaction != null)
        {
            command.Transaction = transaction;
        }

        if (parameters != null)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                OfflineDatabaseSqlParameter parameter = parameters[i];
                if (parameter == null || string.IsNullOrEmpty(parameter.Name))
                {
                    continue;
                }

                IDbDataParameter dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Name;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }
        }

        return command;
    }
}
