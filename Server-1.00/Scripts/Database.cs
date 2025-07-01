using System;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Database
    {

        #region MySQL
        
        private static MySqlConnection _mysqlConnection;
        private const string _mysqlServer = "127.0.0.1";
        private const string _mysqlUsername = "root";
        private const string _mysqlPassword = "";
        private const string _mysqlDatabase = "clash_of_whatever";

        public static MySqlConnection mysqlConnection
        {
            get
            {
                if (_mysqlConnection == null || _mysqlConnection.State == ConnectionState.Closed)
                {
                    try
                    {
                        _mysqlConnection = new MySqlConnection("SERVER=" + _mysqlServer + "; DATABASE=" + _mysqlDatabase + "; UID=" + _mysqlUsername + "; PASSWORD=" + _mysqlPassword + ";");
                        _mysqlConnection.Open();
                        Console.WriteLine("Connection established with MySQL database.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to connect the MySQL database.");
                    }
                }
                else if (_mysqlConnection.State == ConnectionState.Broken)
                {
                    try
                    {
                        _mysqlConnection.Close();
                        _mysqlConnection = new MySqlConnection("SERVER=" + _mysqlServer + "; DATABASE=" + _mysqlDatabase + "; UID=" + _mysqlUsername + "; PASSWORD=" + _mysqlPassword + ";");
                        _mysqlConnection.Open();
                        Console.WriteLine("Connection re-established with MySQL database.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to connect the MySQL database.");
                    }
                }
                return _mysqlConnection;
            }
        }
        

        public async static void AuthenticatePlayer(int id, string device) {
            long account_id = await AuthenticatePlayerAsync(id, device);
            Server.clients[id].device = device;
            Server.clients[id].account = account_id;
            Packet packet = new Packet();
            packet.Write((int) Terminal.RequestsID.AUTH);
            packet.Write(account_id);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<long> AuthenticatePlayerAsync(int id, string device) {
            Task<long> task = Task.Run(() => 
            {
                long account_id = 0;
                string query = String.Format("SELECT id FROM accounts WHERE device_id = '{0}';", device);
                bool found = false;
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                account_id = long.Parse(reader["id"].ToString());
                                found = true;
                            }
                        }
                    }
                }
                if (!found) {
                    query =  String.Format("INSERT INTO accounts (device_id) VALUES ('{0}');", device);
                    using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                    {
                        command.ExecuteNonQuery();
                        account_id = command.LastInsertedId;
                    }
                }    
                return account_id;
            });
            return await task;
        }

        public async static void SyncPlayerData(int id, string device) {
            long account_id = Server.clients[id].account;
            Data.Player player = await GetPlayerDataAsync(id, device);
            List<Data.Building> buildings = await GetBuildingsAsync(account_id);
            player.buildings = buildings;
            
            Packet packet = new Packet();
            packet.Write((int)Terminal.RequestsID.SYNC);
            string playerData = await Data.Serialize<Data.Player>(player);
            packet.Write(playerData);
            Sender.TCP_Send(id, packet);
        }

        private async static Task<Data.Player> GetPlayerDataAsync(int id, string device) {
            Task<Data.Player> task = Task.Run(() => 
            {
                Data.Player data = new Data.Player();
                string query = String.Format("SELECT id, gold, elixir, gems FROM accounts WHERE device_id = '{0}';", device);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                // data.id = long.Parse(reader["id"].ToString());
                                data.gold = int.Parse(reader["gold"].ToString());
                                data.elixir = int.Parse(reader["elixir"].ToString());
                                data.gems = int.Parse(reader["gems"].ToString());
                            }
                        }
                    }
                }
                
                return data;
            });
            return await task;
        }

        public async static void PlaceBuilding(int id, string device, string buildingID, int x, int y) {
            Packet packet = new Packet();
            packet.Write(3);
            Data.Player player = await GetPlayerDataAsync(id, device);
            Data.ServerBuilding building = await GetServerBuildingAsync(buildingID, 1);
            if(player.gold >= building.requiredGold && player.elixir >= building.requiredElixir && player.gems >= building.requiredGems) {
                long account_id = Server.clients[id].account;
                List<Data.Building> buildings = await GetBuildingsAsync(account_id);
                bool canPlaceBuilding = true;
                
                if (x < 0 || y < 0 || x + building.columns > 45 || y + building.rows > 45)                 {
                    canPlaceBuilding = false;
                } else {
                    
                    for ( int i = 0 ; i < buildings.Count ; i++) {
                        Rectangle rect1 = new Rectangle(buildings[i].x, buildings[i].y, buildings[i].columns, buildings[i].rows);
                        Rectangle rect2 = new Rectangle(x, y, building.columns, building.rows);

                        if (rect2.IntersectsWith(rect1)) {
                            canPlaceBuilding = false;
                        }
                    }
                    
                }
                if (canPlaceBuilding) {
                    long building_id = await PlaceBuildingAsync(account_id, building, x, y);
                    packet.Write(1);
                } else {
                    packet.Write(2);
                }
            }
            else {
                packet.Write(0);
            }
            Sender.TCP_Send(id, packet);
        }

        private async static Task<long> PlaceBuildingAsync(long account_id, Data.ServerBuilding building, int x, int y) {
            Task<long> task = Task.Run(() => 
            {
                long id = 0;
                string query = String.Format("UPDATE accounts SET gold = gold - {0}, elixir = elixir - {1}, gems = gems - {2} WHERE id = {3};", building.requiredGold, building.requiredElixir, building.requiredGems, account_id);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.ExecuteNonQuery();
                }
                query = String.Format("INSERT INTO buildings (global_id, account_id, x_position, y_position, columns_count, rows_count) VALUES('{0}', {1}, {2}, {3}, {4}, {5});", building.id, account_id, x, y, building.columns, building.rows);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.ExecuteNonQuery();
                    id = command.LastInsertedId;
                }
                return account_id;
            });
            return await task;
        }


        private async static Task<Data.Building> GetBuildingAsync(long account, string id) {
            Task<Data.Building> task = Task.Run(() => 
            {
                Data.Building data = new Data.Building();
                data.id = id;
                string query = String.Format("SELECT id, level, x_position, y_position, columns_count, rows_count FROM buildings WHERE account_id = {0} AND global_id = '{1}';", account, id);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                data.databaseID = long.Parse(reader["id"].ToString());
                                data.level = int.Parse(reader["level"].ToString());
                                data.x = int.Parse(reader["x_position"].ToString());
                                data.y = int.Parse(reader["y_position"].ToString());
                                data.columns = int.Parse(reader["columns_count"].ToString());
                                data.rows = int.Parse(reader["rows_count"].ToString());
                            }
                        }
                    }
                }
                
                return data;
            });
            return await task;
        }


        private async static Task<Data.ServerBuilding> GetServerBuildingAsync(string id, int level) {
            Task<Data.ServerBuilding> task = Task.Run(() => 
            {
                Data.ServerBuilding data = new Data.ServerBuilding();
                data.id = id;
                string query = String.Format("SELECT id, req_gold, req_elixir, req_gems, columns_count, rows_count FROM server_buildings WHERE global_id = '{0}' AND level = '{1}';", id, level);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                data.databaseID = long.Parse(reader["id"].ToString());
                                data.level = level;
                                data.requiredGold = int.Parse(reader["req_gold"].ToString());
                                data.requiredElixir = int.Parse(reader["req_elixir"].ToString());
                                data.requiredGems = int.Parse(reader["req_gems"].ToString());
                                data.columns = int.Parse(reader["columns_count"].ToString());
                                data.rows = int.Parse(reader["rows_count"].ToString());
                            }
                        }
                    }
                }
                
                return data;
            });
            return await task;
        }

        private async static Task<List<Data.Building>> GetBuildingsAsync(long account) {
            Task<List<Data.Building>> task = Task.Run(() => 
            {
                List<Data.Building> data = new List<Data.Building>();
                string query = String.Format("SELECT id, global_id, level, x_position, y_position, columns_count, rows_count FROM buildings WHERE account_id = '{0}';", account);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.Building building = new Data.Building();
                                building.id = reader["global_id"].ToString();
                                building.databaseID = long.Parse(reader["id"].ToString());
                                building.level = int.Parse(reader["level"].ToString());
                                building.x = int.Parse(reader["x_position"].ToString());
                                building.y = int.Parse(reader["y_position"].ToString());
                                building.columns = int.Parse(reader["columns_count"].ToString());
                                building.rows = int.Parse(reader["rows_count"].ToString());
                                data.Add(building);
                            }
                        }
                    }
                }
                
                return data;
            });
            return await task;
        }

        
        #endregion

        #region SQL
        /*
        private static SqlConnection _sqlConnection;
        private const string _sqlServer = "server";
        private const string _sqlDatabase = "database";

        public static SqlConnection sqlConnection
        {
            get
            {
                if (_sqlConnection == null || _sqlConnection.State == ConnectionState.Closed)
                {
                    try
                    {
                        var connectionString = @"Server=localhost\" + _sqlServer + ";Database=" + _sqlDatabase + ";Initial Catalog=" + _sqlDatabase + ";Trusted_Connection=True;MultipleActiveResultSets=true";
                        _sqlConnection = new SqlConnection(connectionString);
                        _sqlConnection.Open();
                        Console.WriteLine("Connection established with SQL database.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to connect the SQL database.");
                    }
                }
                else if (_sqlConnection.State == ConnectionState.Broken)
                {
                    try
                    {
                        _sqlConnection.Close();
                        var connectionString = @"Server=localhost\" + _sqlServer + ";Database=" + _sqlDatabase + ";Initial Catalog=" + _sqlDatabase + ";Trusted_Connection=True;MultipleActiveResultSets=true";
                        _sqlConnection = new SqlConnection(connectionString);
                        _sqlConnection.Open();
                        Console.WriteLine("Connection re-established with SQL database.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to connect the SQL database.");
                    }
                }
                return _sqlConnection;
            }
        }

        public static void Demo_SQL_1()
        {
            string query = String.Format("UPDATE database.table SET int_column = {0}, string_column = '{1}', datetime_column = GETUTCDATE();", 123, "Hello World");
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public static void Demo_SQL_2()
        {
            string query = String.Format("SELECT column1, column2 FROM database.table WHERE column3 = {0} ORDER BY column1 DESC;", 123);
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int column1 = int.Parse(reader["column1"].ToString());
                            string column2 = reader["column2"].ToString();
                        }
                    }
                }
            }
        }
        */
        #endregion

    }
}