using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Youtuber2._0
{
    public class DBConnection
    {
        public static string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""I:\My Drive\Visual Studio Projects\Milanw2\Youtuber2.0\Youtuber2.0\YoutuberDB.mdf"";Integrated Security=True";
        public static SqlConnection con = new SqlConnection(ConnectionString);
        public static string GetDestinationFolder()
        {
            con.Open();
            string returnvalue = "";

            SqlCommand cmd = new SqlCommand("SELECT text FROM CONFIGURATIONS WHERE CODE = 'SETTINGS' AND SUB_CODE = 'DESTINATION'", con);
            SqlDataReader da = cmd.ExecuteReader();
            while (da.Read())
            {
                returnvalue = da.GetValue(0).ToString();
            }

            con.Close();

            return returnvalue;
        }
        public static void SetDestinationFolder(string destination)
        {
            con.Open();

            SqlCommand cmd = new SqlCommand("UPDATE CONFIGURATIONS SET TEXT = @text WHERE CODE = 'SETTINGS' AND SUB_CODE = 'DESTINATION'", con);
            cmd.Parameters.Add("@text", SqlDbType.NVarChar);
            cmd.Parameters["@text"].Value = destination;

            cmd.ExecuteNonQuery();

            con.Close();
        }
        public static List<string> GetPlaylistIdsArray()
        {
            con.Open();
            List<string> returnvalue = new List<string>();

            SqlCommand cmd = new SqlCommand("SELECT id FROM PLAYLIST_IDS", con);
            SqlDataReader da = cmd.ExecuteReader();
            while (da.Read())
            {
                returnvalue.Add(da.GetValue(0).ToString());
            }

            con.Close();

            return returnvalue;
        }
        public static void DeletePlaylistIdById(string id)
        {
            SqlCommand cmd = new SqlCommand("DELETE PLAYLIST_IDS WHERE ID = @ID", con);
            cmd.Parameters.Add("@ID", SqlDbType.NVarChar);
            cmd.Parameters["@ID"].Value = id;

            con.Open();

            cmd.ExecuteNonQuery();

            con.Close();
        }
        public static void InsertPlaylistIdById(string title, string id)
        {
            SqlCommand cmd = new SqlCommand("INSERT INTO PLAYLIST_IDS VALUES (@TITLE, @ID)", con);
            cmd.Parameters.Add("@TITLE", SqlDbType.NVarChar);
            cmd.Parameters.Add("@ID", SqlDbType.NVarChar);
            cmd.Parameters["@TITLE"].Value = title;
            cmd.Parameters["@ID"].Value = id;

            con.Open();

            cmd.ExecuteNonQuery();

            con.Close();
        }
    }
}
