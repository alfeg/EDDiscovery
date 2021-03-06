﻿
using EDDiscovery.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace EDDiscovery2.DB
{
    public class TravelLogUnit
    {
        public long id;
        public string Name;
        public int type;
        public int Size;
        public string Path;


        public TravelLogUnit()
        {
        }

        public TravelLogUnit(DataRow dr)
        {
            id = (long)dr["id"];
            Name = (string)dr["Name"];
            type = (int)(long)dr["type"];
            Size = (int)(long)dr["size"];
            Path = (string)dr["Path"];

        }

        public TravelLogUnit(DbDataReader dr)
        {
            id = (long)dr["id"];
            Name = (string)dr["Name"];
            type = (int)(long)dr["type"];
            Size = (int)(long)dr["size"];
            Path = (string)dr["Path"];
        }

        public bool Beta
        {
            get
            {
                if (Path == null)
                    return false;

                if (Path.Contains("PUBLIC_TEST_SERVER"))
                    return true;
                else
                    return false;
            }
        }



        public bool Add()
        {
            using (SQLiteConnectionED cn = new SQLiteConnectionED())
            {
                bool ret = Add(cn);
                return ret;
            }
        }

        private bool Add(SQLiteConnectionED cn)
        {
            using (DbCommand cmd = cn.CreateCommand("Insert into TravelLogUnit (Name, type, size, Path) values (@name, @type, @size, @Path)"))
            {
                cmd.AddParameterWithValue("@name", Name);
                cmd.AddParameterWithValue("@type", type);
                cmd.AddParameterWithValue("@size", Size);
                cmd.AddParameterWithValue("@Path", Path);

                SQLiteDBClass.SQLNonQueryText(cn, cmd);

                using (DbCommand cmd2 = cn.CreateCommand("Select Max(id) as id from TravelLogUnit"))
                {
                    id = (long)SQLiteDBClass.SQLScalar(cn, cmd2);
                }

                return true;
            }
        }

        public bool Update()
        {
            using (SQLiteConnectionED cn = new SQLiteConnectionED())
            {
                return Update(cn);
            }
        }

        public bool Update(SQLiteConnectionED cn, DbTransaction tn = null)
        {
            using (DbCommand cmd = cn.CreateCommand("Update TravelLogUnit set Name=@Name, Type=@type, size=@size, Path=@Path  where ID=@id", tn))
            {
                cmd.AddParameterWithValue("@ID", id);
                cmd.AddParameterWithValue("@Name", Name);
                cmd.AddParameterWithValue("@Type", type);
                cmd.AddParameterWithValue("@size", Size);
                cmd.AddParameterWithValue("@Path", Path);

                SQLiteDBClass.SQLNonQueryText(cn, cmd);

                return true;
            }
        }
        
        static public List<TravelLogUnit> GetAll()
        {
            List<TravelLogUnit> list = new List<TravelLogUnit>();

            using (SQLiteConnectionED cn = new SQLiteConnectionED())
            {
                using (DbCommand cmd = cn.CreateCommand("select * from TravelLogUnit"))
                {
                    DataSet ds = SQLiteDBClass.SQLQueryText(cn, cmd);
                    if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                        return list;

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        TravelLogUnit sys = new TravelLogUnit(dr);
                        list.Add(sys);
                    }

                    return list;
                }
            }
        }

        public static List<string> GetAllNames()
        {
            List<string> names = new List<string>();
            using (SQLiteConnectionED cn = new SQLiteConnectionED())
            {
                using (DbCommand cmd = cn.CreateCommand("SELECT DISTINCT Name FROM TravelLogUnit"))
                {
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add((string)reader["Name"]);
                        }
                    }
                }
            }
            return names;
        }

        public static TravelLogUnit Get(string name)
        {
            using (SQLiteConnectionED cn = new SQLiteConnectionED())
            {
                using (DbCommand cmd = cn.CreateCommand("SELECT * FROM TravelLogUnit WHERE Name = @name ORDER BY Id DESC"))
                {
                    cmd.AddParameterWithValue("@name", name);
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TravelLogUnit(reader);
                        }
                    }
                }
            }

            return null;
        }

        public static bool TryGet(string name, out TravelLogUnit tlu)
        {
            tlu = Get(name);
            return tlu != null;
        }
    }
}

