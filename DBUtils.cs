using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IBM.Data.DB2.iSeries;
using System.Configuration;
using ScottWatch.Models;
using System.Data.SqlClient;
using System.Data;



namespace ScottWatch
{


    public class DBUtils
    {

        public static string DomainServer = "ssss";
        public static string AppDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string DB2ConnectionString = ConfigurationManager.AppSettings["DB2ConnectionString"];
        public static string connectionString = ConfigurationManager.AppSettings["transfertablesDSN"];
        public static string dbName = ConfigurationManager.AppSettings["dbName"];  //dbName = "ALTLIB";
        public static string Warehouse = ConfigurationManager.AppSettings["Warehouse"];

        public static string get_branchPickString = ConfigurationManager.AppSettings["get_branchPickString"];
        public static string get_branchPutString = ConfigurationManager.AppSettings["get_branchPutString"];
        public static string get_branchPartmasterString = ConfigurationManager.AppSettings["get_branchPartmasterString"];

        public static string CheckForNewData()
        {
            File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "CheckForNewData AppDir-" + AppDir + " " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));
            //File.AppendAllText(AppDir + @"\ServiceLog-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "CheckForNewData DB2ConnectionString-" + DB2ConnectionString + " " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));

            //File.AppendAllText(AppDir + @"\ServiceLog-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "CheckForNewData SQLconnectionString-" + connectionString + " " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"));

            string results = "";
            try
            {
                // build Lists for Pick And Put
                List<PickOrders> PickList = GetPickOrders();
                List<PutOrders> PutList = GetPutOrders();

                //Chk SQL Orders to see if Top record in each list exists
                // If exist, Jump out, else 
                //Re-Start Loop and add to Orders until you get to list item that does exist, the Jump

                var iPickInsertCount = 0;
                var iPutInsertCount = 0;

                //Picks
                for (int i = 0; i < PickList.Count; i++)
                {
                    List<PickOrders> pickOrder = new List<PickOrders>();
                    pickOrder.Add(PickList[i]);

                    bool IsPickInSql = CheckSQLPICK(pickOrder);
                    if (!IsPickInSql)
                    {
                        //Add it to SQL
                        InsertSQLPICK(pickOrder);
                        iPickInsertCount++;
                    }
                    else
                    {
                        //If found we are ready to jump out of loop
                        //break;
                        //do nothing but dont Break
                    }
                }
                //Put
                iPutInsertCount = 0;
                for (int i = 0; i < PutList.Count; i++)
                {
                    List<PutOrders> putOrder = new List<PutOrders>();
                    putOrder.Add(PutList[i]);

                    bool IsPutInSql = CheckSQLPUT(putOrder);
                    if (!IsPutInSql)
                    {
                        //Add it to SQL
                        InsertSQLPUT(putOrder);
                        iPutInsertCount++;
                    }
                    else
                    {
                        //If found we are ready to jump out of loop
                        //break;
                        //do nothing but dont Break
                    }
                }



                results = "Instered [" + iPickInsertCount.ToString() + "]Pick Orders / [" + iPutInsertCount.ToString() + "]Put Orders";
                return results;

            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Work Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
                results = "ERROR";
                return results;
            }
        }

        public static string CheckForNewData_PM()
        {
            string results = "";
            try
            {
                List<PartsMaster> partsMaster = GetPartsMaster();
                var iPartInsertCount = 0;
                var iPartUpdateCount = 0;

                //Picks
                for (int i = 0; i < partsMaster.Count; i++)
                {
                    List<PartsMaster> part = new List<PartsMaster>();
                    part.Add(partsMaster[i]);

                    bool IsPartInSql = CheckSQLPart(part);
                    if (!IsPartInSql)
                    {
                        //Add it to SQL
                        InsertSQLPart(part);
                        iPartInsertCount++;
                    }
                    else
                    {
                        //Update SQL 
                        UpdateSQLPart(part);
                        iPartUpdateCount++;

                    }
                }

                results = "Instered [" + iPartInsertCount.ToString() + "] Parts / Updated [" + iPartUpdateCount.ToString() + "] Parts";
                return results;

            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPM-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "PMService Work Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
                results = "ERROR";
                return results;
            }


        }
        public static List<PartsMaster> GetPartsMaster()
        {
            iDB2Connection conn = new iDB2Connection(DB2ConnectionString);
            try
            {
                List<PartsMaster> partsMaster = new List<PartsMaster>();
                conn.Open();
                if (conn != null)
                {
                    Console.WriteLine("Successfully connected...");
                    //string qry = "SELECT Count(*)  FROM " + dbName + ".SALORD";
                    string qry = "SELECT * FROM " + dbName + ".PARTMAST Where PMSTA = 'A' and "+ get_branchPartmasterString + "  and PMBIN = 'KARDEX' order by PMDTA DESC";
                    iDB2Command comm = conn.CreateCommand();
                    comm.CommandText = qry;
                    iDB2DataReader reader = comm.ExecuteReader();

                    while (reader.Read())
                    {
                        //results = reader[0].ToString();

                        PartsMaster pm = new PartsMaster();

                        pm.PM_VEN = reader["PM_VEN"].ToString();
                        pm.PM_PRT = reader["PM_PRT"].ToString();
                        pm.PM_DES = reader["PM_DES"].ToString();
                        pm.PM_WT = reader["PM_WT"].ToString();
                        pm.PM_OH = reader["PM_OH"].ToString();
                        pm.PM_QA = reader["PM_QA"].ToString();
                        pm.PM_QR = reader["PM_QR"].ToString();
                        pm.PM_DTA = reader["PM_DTA"].ToString();
                        pm.PM_NET = reader["PM_NET"].ToString();
                        pm.NetOnHand = (reader.GetInt32(reader.GetOrdinal("PM_OH")) - reader.GetInt32(reader.GetOrdinal("PM_QA")) - reader.GetInt32(reader.GetOrdinal("PM_QR"))).ToString();

                        partsMaster.Add(pm);
                    }
                    reader.Close();
                    comm.Dispose();
                }
                return partsMaster;
            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPM-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Work Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
                return null;


            }
            finally
            {
                conn.Close();
            }

        }

        public static List<PickOrders> GetPickOrders()
        {

            iDB2Connection conn = new iDB2Connection(DB2ConnectionString);

            try
            {
                List<PickOrders> pickOrders = new List<PickOrders>();
                conn.Open();
                if (conn != null)
                {
                    Console.WriteLine("Successfully connected...");
                    //string qry = "SELECT Count(*)  FROM " + dbName + ".SALORD";
                    string qry = "SELECT * FROM " + dbName + ".SALORD A, " + dbName + ".SALDET B where A.SO_ORD = B.SD_ORD and B.SD_BIN = 'KARDEX' and A.SO_STA = 'A' and "+ get_branchPickString + "  and  A.SO_VIA <> '$W/O ADJ$' order by SD_DTM DESC, SD_TMO DESC fetch first 500 rows only ";
                    //string qry = "SELECT * FROM " + dbName + ".SALORD A, " + dbName + ".SALDET B where A.SO_ORD = B.SD_ORD and B.SD_BIN = 'KARDEX' and A.SO_STA = 'A' and A.SO_BR = 83  order by SO_DTO DESC, SD_TMO DESC fetch first 100 rows only ";
                    iDB2Command comm = conn.CreateCommand();
                    comm.CommandText = qry;
                    iDB2DataReader reader = comm.ExecuteReader();



                    while (reader.Read())
                    {
                        //results = reader[0].ToString();
                        PickOrders pick = new PickOrders();
                        pick.SO_ORD = reader["SO_ORD"].ToString();
                        pick.SD_LNE = reader["SD_LNE"].ToString();
                        pick.SD_PRT = reader["SD_PRT"].ToString();
                        pick.SD_TISS = reader["SD_TISS"].ToString();
                        pick.SO_DTO = reader["SO_DTO"].ToString();
                        pick.SD_TMO = reader["SD_TMO"].ToString();
                        pick.SD_BIN = reader["SD_BIN"].ToString();
                        pick.SD_PO = reader["SD_PO"].ToString();
                        pick.SO_BR = reader["SO_BR"].ToString();

                        pickOrders.Add(pick);
                    }
                    reader.Close();
                    comm.Dispose();
                }
                return pickOrders;
            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Work Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
                return null;


            }
            finally
            {
                conn.Close();
            }


        }
        public static List<PutOrders> GetPutOrders()
        {
            iDB2Connection conn = new iDB2Connection(DB2ConnectionString);

            try
            {
                List<PutOrders> putOrders = new List<PutOrders>();
                conn.Open();
                if (conn != null)
                {
                    Console.WriteLine("Successfully connected...");
                    string qry = "SELECT * FROM " + dbName + ".PARTHIST where PHSTA = 'R' and " + get_branchPutString + " and PHDTR  >= '20180424' and PHTYP <> 'MD' order by PH_DTR DESC, PH_TMR DESC fetch first 100 rows only ";
                    iDB2Command comm = conn.CreateCommand();
                    comm.CommandText = qry;
                    iDB2DataReader reader = comm.ExecuteReader();



                    while (reader.Read())
                    {
                        PutOrders put = new PutOrders();
                        put.PH_PO = reader["PH_PO"].ToString();
                        put.PH_PRT = reader["PH_PRT"].ToString();
                        put.PH_QTR = reader["PH_QTR"].ToString();
                        put.PH_VEN = reader["PH_VEN"].ToString();
                        put.PH_DTR = reader["PH_DTR"].ToString();
                        put.PH_BR = reader["PH_BR"].ToString();

                        putOrders.Add(put);
                    }
                    reader.Close();
                    comm.Dispose();
                }
                return putOrders;
            }
            catch (Exception ex)
            {
                File.AppendAllText(AppDir + @"\ServiceLogPP-" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt", Environment.NewLine + "Service Work Error: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + ex.Message);
                return null;

            }
            finally
            {
                conn.Close();
            }


        }

        public static void InsertSQLPICK(List<PickOrders> PickPart)

        {

            int sqlCount = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT into Orders ([MasterOrder.MasterOrderName],  [MasterOrderLine.MaterialName], [MasterOrder.DirectionType] ,[MasterOrderLine.LineNumber],  [MasterOrderLine.Quantity], ImportDate, [Masterorder.Warehouse])";
                    command.CommandText += " VALUES (@MasterOrderName, @MaterialName, @DirectionType, @LineNumber, @Quantity, @ImportDate, @Warehouse)";
                    command.Parameters.AddWithValue("@MasterOrderName", PickPart[0].SO_ORD);
                    command.Parameters.AddWithValue("@MaterialName", PickPart[0].SD_PRT);

                    int numVal = Int32.Parse(PickPart[0].SD_TISS);
                    if (numVal < 0)
                    {
                        //If negative the ITS A PUT
                        command.Parameters.AddWithValue("@DirectionType", 1);
                        numVal = Math.Abs(Int32.Parse(PickPart[0].SD_TISS));
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@DirectionType", 2);
                    }

                    command.Parameters.AddWithValue("@LineNumber", PickPart[0].SD_LNE);
                    command.Parameters.AddWithValue("@Quantity", numVal.ToString());
                    command.Parameters.AddWithValue("@ImportDate", PickPart[0].SO_DTO);
                    //command.Parameters.AddWithValue("@Warehouse", Warehouse);
                    command.Parameters.AddWithValue("@Warehouse", PickPart[0].SO_BR);

                    try
                    {
                        //numVal = PickPart[0].SD_TISS
                        if (numVal != 0) {
                            if (PickPart[0].SD_PO.Trim() == "") //If has Value then Skip Insert
                            { 
                                connection.Open();
                                sqlCount = command.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        // error here
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

        }

        public static void InsertSQLPUT(List<PutOrders> PutPart)

        {

            int sqlCount = 0;
            int maxlineNumber = 0;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string cmdData = "";
                cmdData = "Select Max([MasterOrderLine.LineNumber]) from Orders Where  [MasterOrder.MasterOrderName] = '" + PutPart[0].PH_PO + "'";
                using (SqlCommand command = new SqlCommand(cmdData, con))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader[0].ToString() == "")
                        {
                            maxlineNumber = 0;
                        }
                        else
                        {
                            maxlineNumber = reader.GetInt32(0);
                        }

                    }
                }
                con.Close();
            }
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    maxlineNumber++;
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT into Orders ([MasterOrder.MasterOrderName],  [MasterOrderLine.MaterialName], [MasterOrder.DirectionType] ,[MasterOrderLine.LineNumber],  [MasterOrderLine.Quantity], ImportDate, [Masterorder.Warehouse])";
                    command.CommandText += " VALUES (@MasterOrderName, @MaterialName, @DirectionType, @LineNumber, @Quantity, @ImportDate, @Warehouse)";
                    command.Parameters.AddWithValue("@MasterOrderName", PutPart[0].PH_PO);
                    command.Parameters.AddWithValue("@MaterialName", PutPart[0].PH_PRT);
                    command.Parameters.AddWithValue("@DirectionType", 1);
                    command.Parameters.AddWithValue("@LineNumber", maxlineNumber);
                    command.Parameters.AddWithValue("@Quantity", PutPart[0].PH_QTR);
                    command.Parameters.AddWithValue("@ImportDate", PutPart[0].PH_DTR);
                    //command.Parameters.AddWithValue("@Warehouse", Warehouse);
                    command.Parameters.AddWithValue("@Warehouse", PutPart[0].PH_BR);

                    try
                    {
                        connection.Open();
                        sqlCount = command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        // error here
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

        }

        public static void InsertSQLPart(List<PartsMaster> PutPart)
        {

            int sqlCount = 0;
            int maxlineNumber = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    maxlineNumber++;
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "INSERT into PartsMaster ( PMVEN, PMPRT,PMDES,PMWT,PMNETONHAND,PMDTA, Warehouse,PMNET)";
                    command.CommandText += " VALUES (@PMVEN, @PMPRT, @PMDES, @PMWT, @PMNETONHAND, @PMDTA, @Warehouse, @PMNET)";
                    command.Parameters.AddWithValue("@PMVEN", PutPart[0].PM_VEN);
                    command.Parameters.AddWithValue("@PMPRT", PutPart[0].PM_PRT);
                    command.Parameters.AddWithValue("@PMDES", PutPart[0].PM_DES);
                    command.Parameters.AddWithValue("@PMWT", PutPart[0].PM_WT);
                    command.Parameters.AddWithValue("@PMDTA", PutPart[0].PM_DTA);
                    

                    // PMOH - PMQA - PMQR
                    Int32.TryParse(PutPart[0].PM_OH, out int PMOH);
                    Int32.TryParse(PutPart[0].PM_QA, out int PMQA);
                    Int32.TryParse(PutPart[0].PM_QR, out int PMQR);
                    command.Parameters.AddWithValue("@PMNETONHAND", (PMOH - PMQA - PMQR).ToString());
                    //command.Parameters.AddWithValue("@Warehouse", Warehouse);
                    command.Parameters.AddWithValue("@Warehouse", PutPart[0].PM_BR);
                    command.Parameters.AddWithValue("@PMNET", PutPart[0].PM_NET);


                    try
                    {
                        connection.Open();
                        sqlCount = command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        // error here
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

        }
        public static void UpdateSQLPart(List<PartsMaster> PutPart)
        {

            int sqlCount = 0;
            int maxlineNumber = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    maxlineNumber++;
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "Update PartsMaster  SET PMVEN = @PMVEN,PMPRT =  @PMPRT,PMDES = @PMDES,PMWT = @PMWT,PMNETONHAND = @PMNETONHAND, PMDTA = @PMDTA, Warehouse = @Warehouse,  PMNET = @PMNET  Where  PMPRT = '" + PutPart[0].PM_PRT + "'";

                    command.Parameters.AddWithValue("@PMVEN", PutPart[0].PM_VEN);
                    command.Parameters.AddWithValue("@PMPRT", PutPart[0].PM_PRT);
                    command.Parameters.AddWithValue("@PMDES", PutPart[0].PM_DES);
                    command.Parameters.AddWithValue("@PMWT", PutPart[0].PM_WT);
                    command.Parameters.AddWithValue("@PMDTA", PutPart[0].PM_DTA);

                    // PMOH - PMQA - PMQR
                    Int32.TryParse(PutPart[0].PM_OH, out int PMOH);
                    Int32.TryParse(PutPart[0].PM_QA, out int PMQA);
                    Int32.TryParse(PutPart[0].PM_QR, out int PMQR);
                    command.Parameters.AddWithValue("@PMNETONHAND", (PMOH - PMQA - PMQR).ToString());
                    //command.Parameters.AddWithValue("@Warehouse", Warehouse);
                    command.Parameters.AddWithValue("@Warehouse", PutPart[0].PM_BR);
                    command.Parameters.AddWithValue("@PMNET", PutPart[0].PM_NET);

                    try
                    {
                        connection.Open();
                        sqlCount = command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        // error here
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

        }

        public static bool CheckSQLPICK(List<PickOrders> PickPart)
        {

            int sqlCount = 0;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string cmdData = "";
                cmdData = "Select Count(*) from Orders Where  [MasterOrder.MasterOrderName] = '" + PickPart[0].SO_ORD + "' and  [MasterOrderLine.LineNumber] = " + PickPart[0].SD_LNE;
                using (SqlCommand command = new SqlCommand(cmdData, con))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sqlCount = reader.GetInt32(0);
                    }
                }
                con.Close();
            }
            if (sqlCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }




        }
        public static bool CheckSQLPart(List<PartsMaster> PickPart)
        {

            int sqlCount = 0;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string cmdData = "";
                cmdData = "Select Count(*) from PartsMaster Where  PMPRT = '" + PickPart[0].PM_PRT + "'";
                using (SqlCommand command = new SqlCommand(cmdData, con))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sqlCount = reader.GetInt32(0);
                    }
                }
                con.Close();
            }
            if (sqlCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }




        }
        public static bool CheckSQLPUT(List<PutOrders> PutPart)
        {

            int sqlCount = 0;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string cmdData = "";
                cmdData = "Select Count(*) from Orders Where  [MasterOrder.MasterOrderName] = '" + PutPart[0].PH_PO + "' and  [MasterOrderLine.MaterialName] = '" + PutPart[0].PH_PRT + "'";
                using (SqlCommand command = new SqlCommand(cmdData, con))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sqlCount = reader.GetInt32(0);
                    }
                }
                con.Close();
            }
            if (sqlCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }




        }

    }
}
