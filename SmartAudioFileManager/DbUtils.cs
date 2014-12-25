using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SmartAudioFileManager
{
    internal static class DbUtils
    {
        #region Variables

        private static SQLiteConnection AudioDbConnection = null;

        private static readonly object MyLock = new object();
        //to store in config file
        private static readonly string DbString = "audio_db.sqlite";

        private static readonly string DbConnection = "Data Source={0};Version=3;";

        //pool/array of command

        #endregion

        #region Manage Connection

        private static SQLiteConnection GetConnection()
        {
            lock (MyLock)
            {
                if (AudioDbConnection == null)
                {
                    CheckCreateDB(Directory.GetCurrentDirectory(), DbString);

                    try
                    {
                        AudioDbConnection = new SQLiteConnection(String.Format(DbConnection, DbString));
                    }
                    catch (Exception e)
                    { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), e); }
                }

                OpenConnection();

                return AudioDbConnection;
            }
        }

        private static void CheckCreateDB(string dbPath, string dbName)
        {
            string FullPath = dbPath + "\\" + dbName;

            if (!File.Exists(FullPath))
            {
                try
                {
                    SQLiteConnection.CreateFile(dbName);
                }
                catch (Exception e)
                { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), e); }
            }
        }

        private static void OpenConnection()
        {
            if (AudioDbConnection != null && AudioDbConnection.State != System.Data.ConnectionState.Open)
            {
                AudioDbConnection.Open();
            }
        }

        public static void CloseConnection()
        {
            if (AudioDbConnection != null && AudioDbConnection.State != System.Data.ConnectionState.Closed)
            {
                AudioDbConnection.Close();
            }
        }

        #endregion

        #region Execute query

        private static void PrepareStatement(ref SQLiteCommand command, params object[] args)
        {
            foreach (var arg in args)
            {
                SQLiteParameter Field = command.CreateParameter();

                command.Parameters.Add(Field);

                Field.Value = arg;
            }
        }

        internal static void ExecuteNonQuery(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, GetConnection());

            string query = (from SQLiteParameter p in command.Parameters where p != null where p.Value != null select string.Format("Param: {0} = {1},  ", p.ParameterName, p.Value.ToString())).Aggregate(command.CommandText, (current, parameter) => current + parameter);

            try
            {
                command.ExecuteNonQuery();

                //CloseConnection();
            }
            catch (Exception e)
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception(query, e)); }
        }

        internal static void ExecuteNonQuery(string sql, params object[] args)
        {
            SQLiteCommand command = new SQLiteCommand(sql, GetConnection());

            PrepareStatement(ref command, args);

            string query = (from SQLiteParameter p in command.Parameters where p != null where p.Value != null select string.Format("Param: {0} = {1},  ", p.ParameterName, p.Value.ToString())).Aggregate(command.CommandText, (current, parameter) => current + parameter);

            try
            {
                command.ExecuteNonQuery();

                //CloseConnection();
            }
            catch (Exception e)
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception(query, e)); }
        }

        #endregion

        #region Initialyze DB

        internal static void InitialyzeDb()
        {
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS File (file_id INTEGER PRIMARY KEY AUTOINCREMENT, path VARCHAR(200), title VARCHAR(100), extension VARCHAR(4), track VARCHAR(4), album VARCHAR(100), year VARCHAR(4), artists VARCHAR(200), genres VARCHAR(50), hash VARCHAR(200) UNIQUE)");

            ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx1_File_hash ON File(hash)");

            ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx2_File_year ON File(year)");
        }

        #endregion
    }
}