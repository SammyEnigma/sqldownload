/********************************************************************/
/*  Language: C#                                       HCG:   1.0   */
/*                                                                  */
/*  File: SQLDownloader.cs                                          */
/*  Description:                                                    */
/*                                                                  */
/*  Created By:  alexhart        Created At: 10/7/2010 11:22:00 AM  */
/*  Modified By: alexhart       Modified At: 10/7/2010 11:22:00 AM  */
/*                                                                  */
/********************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.IO;
namespace SQLDownload
{
	/// <summary>
	/// SQLDownloader Class Object
	/// </summary>
	public class SQLDownloader
	{
		private String m_ConnectionString;
        private String m_DatabaseName;
        private String m_FileExt;
        private String m_MainDir;
        /// <summary>
		/// Default Constructor
		/// </summary>
		public SQLDownloader(String ConnectionString, String MainDir)
		{

            m_ConnectionString = ConnectionString;
            
            //Parses the database name from the connection string
            SqlConnection sc = new SqlConnection(m_ConnectionString);
            m_DatabaseName = sc.Database;
            sc.Dispose();
            
            m_FileExt = ".sql";
            m_MainDir = MainDir;
		}

        /// <summary>
        /// Downloads any stored procedures into files at the give path
        /// </summary>
        public void DownloadStoredProcedures()
        {
            DownloadObject("Stored Procedures\\", "P");
        }

        /// <summary>
        /// Downloads any Views into files at the give path
        /// </summary>
        public void DownloadViews()
        {
            DownloadObject("Views\\", "V");
        }
        /// <summary>
        /// Downloads any Triggers into files at the give path
        /// </summary>
        public void DownloadTriggers()
        {
            DownloadObject("Triggers\\", "TR");
        }
        /// <summary>
        /// Downloads any Functions into files at the give path
        /// </summary>
        public void DownloadFunctions()
        {
            DownloadObject("Functions\\", "FN");
        }

        /// <summary>
        /// Downloads any Tables into files at the give path
        /// </summary>
        public void DownloadTables()
        {
            DownloadObject("Tables\\", "U");
        }

        /// <summary>
        /// Downloads any stored procedures into files at the give path
        /// </summary>
        public void DownloadAll()
        {
            DownloadTables();
            DownloadFunctions();
            DownloadTriggers();
            DownloadViews();
            DownloadStoredProcedures();
        }

        private void DownloadObject(String PathToDownloadTo, String ObjectType)
        {
            SqlConnection sc = new SqlConnection(m_ConnectionString);
            SqlConnection sc2 = new SqlConnection(m_ConnectionString);
            sc.Open();
            sc2.Open();
            if (!System.IO.Directory.Exists(m_MainDir + PathToDownloadTo))
            {
                System.IO.Directory.CreateDirectory(m_MainDir + PathToDownloadTo);
            }
            SqlCommand scmd = sc.CreateCommand();

            scmd.CommandText = "SELECT * FROM sysobjects WHERE type = '" + ObjectType + "' AND category = 0 ORDER BY name";

            SqlDataReader sdr = scmd.ExecuteReader();
            String objectname = "";
            List<String> objectarray = new List<string>();
            while (sdr.Read())
            {
                objectname = sdr.GetString(0);
                objectarray.Add(objectname);
            }
            sdr.Close();
            sdr.Dispose();
            if (ObjectType != "U")
            {
                foreach (String objname in objectarray)
                {
                    scmd = sc.CreateCommand();
                    scmd.CommandText = "SELECT text FROM syscomments WHERE id = (SELECT id FROM sysobjects WHERE name = '" + objname + "') ORDER BY colid ";
                    sdr = scmd.ExecuteReader();
                    String line;
                    StringBuilder linearray = new StringBuilder();
                    while (sdr.Read())
                    {
                        line = sdr.GetString(0);
                        linearray.Append(line);
                    }
                    sdr.Close();
                    sdr.Dispose();
                    WriteToFile(m_MainDir + PathToDownloadTo + objname + m_FileExt, linearray.ToString());
                }
            }
            else
            {
                foreach (String objname in objectarray)
                {
                    scmd = sc.CreateCommand();
                    scmd.CommandText = "SELECT ORDINAL_POSITION  ,COLUMN_NAME  ,DATA_TYPE  ,CHARACTER_MAXIMUM_LENGTH  ,IS_NULLABLE  ,COLUMN_DEFAULT FROM     INFORMATION_SCHEMA.COLUMNS WHERE     TABLE_NAME = '" + objname + "' ORDER BY   ORDINAL_POSITION ASC;";
                    sdr = scmd.ExecuteReader();
                    String line;
                    StringBuilder linearray = new StringBuilder();
                    linearray.AppendLine("Create Table " + objname);
                    linearray.AppendLine("{");
                    while (sdr.Read())
                    {
                        line = "   " + sdr.GetString(1) + " " + sdr.GetString(2) ;
                        if (!sdr.IsDBNull(3))
                        {
                            line += "(" + sdr.GetInt32(3).ToString() + ")";
                        }
                        if (sdr.GetString(4) == "YES")
                        {
                            line += " NULL,";
                        }
                        else
                        {
                            line += " NOT NULL,";
                        }
                        linearray.AppendLine(line);
                    }
                    sdr.Close();
                    sdr.Dispose();
                    scmd.Dispose();
                    scmd = sc.CreateCommand();
                    scmd.CommandText = "select Constraint_NAME, Constraint_Type FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS A    where Table_Name = '" + objname + "'";
                    sdr = scmd.ExecuteReader();
                    Int32 maincount = 0;
                    while (sdr.Read())
                    {
                        linearray.Append("CONSTRAINT " + sdr.GetString(0) + " " + sdr.GetString(1) + " (");

                        SqlCommand innerscmd = sc2.CreateCommand();
                        innerscmd.CommandText = "select Column_Name FROM   INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE A  where Table_Name = '" + objname + "' and Constraint_NAme = '" + sdr.GetString(0) + "' ";
                        SqlDataReader innersdr = innerscmd.ExecuteReader();
                        Int32 count = 0;
                        while (innersdr.Read())
                        {
                            linearray.Append(innersdr.GetString(0) + ",");
                            count++;
                        }
                        if (count > 0)
                        {
                            linearray.Remove(linearray.Length - 1, 1);
                        }
                        linearray.Append("),\n");
                        innersdr.Close();
                        innersdr.Dispose();
                        innerscmd.Dispose();
                        maincount++;
                       }
                       if (maincount > 0)
                       {
                           linearray.Remove(linearray.Length - 2, 2);
                       }
                    linearray.AppendLine("\n)");
                    sdr.Close();
                    sdr.Dispose();
                    scmd.Dispose();
                    linearray.AppendLine("");
                    
                    scmd = sc.CreateCommand();
                    scmd.CommandText = "exec sp_helpIndex '" + objname + "'";
                    sdr = scmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        String Clustered = "CLUSTERED";
                        String UNIQUE = "UNIQUE";
                        if (!sdr.GetString(1).Contains("unique"))
                            {UNIQUE = "";}
                        if (sdr.GetString(1).Contains("nonclustered"))
                            {Clustered = "NONCLUSTERED";}
                        else if (!sdr.GetString(1).Contains("clustered"))
                            {Clustered = "";}
                        linearray.AppendLine("Create " + UNIQUE + " " + Clustered + " Index " + sdr.GetString(0) + " ON " + objname + " (" + sdr.GetString(2) + ")");
                        linearray.AppendLine("");
                    }
                    sdr.Close();
                    sdr.Dispose();
                    scmd.Dispose();


                    WriteToFile(m_MainDir + PathToDownloadTo + objname + m_FileExt, linearray.ToString());
                }
            }
            sc.Close();
            sc.Dispose();
            sc2.Close();
            sc2.Dispose();
        }

        private void WriteToFile(String file, String text)
        {
            StreamWriter sw = new StreamWriter(file);
            sw.Write(text);
            sw.Close();
            sw.Dispose();
        }
	}
}
