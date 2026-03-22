using SOSApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace SOSApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["dbPaySlipConnection"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Username"] != null)
            {
                return RedirectToAction("EmergencyList");
            }
            Login login = new Login();
            return View(login);
        }

        [HttpPost]
        public ActionResult Index(Login model)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ID, Username, Password FROM Login WHERE Username = @Username AND Password = @Password";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", model.Username);
                    command.Parameters.AddWithValue("@Password", model.Password);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Session["Username"] = reader["Username"].ToString();
                            Session["UserID"] = reader["ID"].ToString();
                            return RedirectToAction("EmergencyList");
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Login Failed";
                            return View();
                        }
                    }
                }
            }
        }

        public ActionResult Emergency(string CoverNo)
        {
            Medical medical = GetMedicalData(CoverNo);
            if (medical == null)
            {
                return Content("Data not found.");
            }
            return View(medical);
        }

        [HttpPost]
        public ActionResult Emergency(Emergency emergency, HttpPostedFileBase Image1, HttpPostedFileBase Image2, HttpPostedFileBase Video, HttpPostedFileBase Audio)
        {
            emergency.CreatedDate = DateTime.Now;

            // Handle Image 1 Upload
            if (Image1 != null && Image1.ContentLength > 0)
            {
                emergency.Image1Path = SaveFile(Image1, "Images");
            }

            // Handle Image 2 Upload
            if (Image2 != null && Image2.ContentLength > 0)
            {
                emergency.Image2Path = SaveFile(Image2, "Images");
            }

            // Handle Video Upload
            if (Video != null && Video.ContentLength > 0)
            {
                emergency.VideoPath = SaveFile(Video, "Videos");
            }

            // Handle Audio Upload
            if (Audio != null && Audio.ContentLength > 0)
            {
                emergency.AudioPath = SaveFile(Audio, "Audio");
            }

            SaveEmergencyData(emergency);
            ViewBag.SuccessMessage = "Emergency case submitted to Gujarat State Haj Committee. Request No " + emergency.ID;
            Medical medical = new Medical();
            return View(medical);
        }

        private string SaveFile(HttpPostedFileBase file, string folder)
        {
            //string fileName = Path.GetFileName(file.FileName);
            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid().ToString() + fileExtension;
            string directory = Server.MapPath("~/Uploads/" + folder);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, fileName);
            file.SaveAs(filePath);
            return "/Uploads/" + folder + "/" + fileName;
        }

        public ActionResult EmergencyList(int page = 1)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Index");
            }

            int pageSize = 20; // Number of records per page
            int totalRecords = GetTotalEmergencyRecords();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            List<Emergency> emergencies = GetPagedEmergencyList(page, pageSize);

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(emergencies);
        }

        private int GetTotalEmergencyRecords()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM tblEmergency";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return (int)command.ExecuteScalar();
                }
            }
        }

        private List<Emergency> GetPagedEmergencyList(int page, int pageSize)
        {
            List<Emergency> emergencies = new List<Emergency>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT ID,ROW_NUMBER() OVER (ORDER BY id desc) AS SrNo, Remarks, CoverNo, PilgrimName, LastName, Address, Latitude, Longitude, CreatedDate, Image1Path, Image2Path, VideoPath, AudioPath, IsResolved
                    FROM tblEmergency where IsResolved=0
                    ORDER BY CreatedDate DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                ";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            List<SHILocation> devices = GetLatestDeviceLocations();

                            decimal? tempLatitude = reader["Latitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Latitude"]);
                            decimal? tempLongitude = reader["Longitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Longitude"]);
                            SHILocation nearestDevice=new SHILocation();
                            try
                            {
                                nearestDevice = FindNearestDevice(devices, (decimal)tempLatitude, (decimal)tempLongitude);
                            }
                            catch
                            {
                                nearestDevice.SHILongitude = 0;
                                nearestDevice.SHILatitude = 0;
                            }

                            //if (nearestDevice != null)
                            //{
                            //    Console.WriteLine($"Nearest Device: {nearestDevice.DeviceName}, Latitude: {nearestDevice.SHILatitude}, Longitude: {nearestDevice.SHILongitude}");
                            //}
                            //else
                            //{
                            //    Console.WriteLine("No devices found.");
                            //}
                            emergencies.Add(new Emergency
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                SrNo = Convert.ToInt32(reader["SrNo"]),
                                CoverNo = reader["CoverNo"].ToString(),
                                PilgrimName = reader["PilgrimName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Address = reader["Address"].ToString(),
                                Latitude = reader["Latitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Latitude"]),
                                Longitude = reader["Longitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Longitude"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                Image1Path = reader["Image1Path"].ToString(),
                                Image2Path = reader["Image2Path"].ToString(),
                                VideoPath = reader["VideoPath"].ToString(),
                                AudioPath = reader["AudioPath"].ToString(),
                                Remarks= reader["Remarks"].ToString(),
                                IsResolved = Convert.ToBoolean(reader["IsResolved"]),
                                SHILatitude = nearestDevice.SHILatitude.HasValue ? nearestDevice.SHILatitude : (decimal?)null,
                                SHILongitude = nearestDevice.SHILongitude.HasValue ? nearestDevice.SHILongitude : (decimal?)null,
                                DeviceName =nearestDevice.DeviceName
                            });
                        }
                    }
                }
            }
            return emergencies;
        }

        static List<SHILocation> GetLatestDeviceLocations()
        {
            List<SHILocation> devices = new List<SHILocation>();

            string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=dbGPS;user id=sa;password=Sai786;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                WITH LatestDevices AS (
                    SELECT DeviceName, Latitude, Longitude,
                           ROW_NUMBER() OVER (PARTITION BY DeviceName ORDER BY DateTime DESC) AS rn
                    FROM [dbGPS].[dbo].[tblLocation]
                )
                SELECT DeviceName, Latitude, Longitude
                FROM LatestDevices
                WHERE rn = 1;";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        devices.Add(new SHILocation
                        {
                            DeviceName = reader["DeviceName"].ToString(),
                            SHILatitude = reader["Latitude"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Latitude"]),
                            SHILongitude = reader["Longitude"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Longitude"])
                        });
                    }
                }
            }

            return devices;
        }

        static SHILocation FindNearestDevice(List<SHILocation> devices, decimal pilgrimLat, decimal pilgrimLon)
        {
            double minDistance = double.MaxValue;
            SHILocation nearestDevice = null;

            foreach (var device in devices)
            {
                double distance = HaversineDistance(pilgrimLat, pilgrimLon, device.SHILatitude, device.SHILongitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestDevice = device;
                }
            }

            return nearestDevice;
        }

        static double HaversineDistance(decimal? lat1, decimal? lon1, decimal? lat2, decimal? lon2)
        {
            const double R = 6371; // Radius of Earth in kilometers

            double dLat = ToRadians((double)(lat2 - lat1));
            double dLon = ToRadians((double)(lon2 - lon1));

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in kilometers
        }

        static double ToRadians(double angle)
        {
            return angle * Math.PI / 180.0;
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }

        private Medical GetMedicalData(string coverNo)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ID,CoverNo, LastName, PilgrimName, FatherName, SpouseName, PassportNo, Address,'' as Remarks FROM Medical WHERE CoverNo = @CoverNo order by 1 desc";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CoverNo", coverNo);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Medical
                            {
                                ID = Convert.ToInt32(reader["ID"].ToString()),
                                CoverNo = reader["CoverNo"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                PilgrimName = reader["PilgrimName"].ToString(),
                                FatherName = reader["FatherName"].ToString(),
                                SpouseName = reader["SpouseName"].ToString(),
                                PassportNo = reader["PassportNo"].ToString(),
                                Address = reader["Address"].ToString(),
                                Remarks = reader["Remarks"].ToString()
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        private void SaveEmergencyData(Emergency emergency)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO tblEmergency (CoverNo, LastName, PilgrimName, Address, Latitude, Longitude, CreatedDate,Remarks,Image1Path, Image2Path, VideoPath, AudioPath) VALUES (@CoverNo, @LastName, @PilgrimName, @Address, @Latitude, @Longitude, @CreatedDate,@Remarks,@Image1Path, @Image2Path, @VideoPath, @AudioPath); SELECT SCOPE_IDENTITY();";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CoverNo", emergency.CoverNo);
                    command.Parameters.AddWithValue("@LastName", emergency.LastName);
                    command.Parameters.AddWithValue("@PilgrimName", emergency.PilgrimName);
                    command.Parameters.AddWithValue("@Address", emergency.Address);
                    command.Parameters.AddWithValue("@Latitude", emergency.Latitude ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Longitude", emergency.Longitude ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedDate", emergency.CreatedDate);
                    command.Parameters.AddWithValue("@Remarks", emergency.Remarks);
                    command.Parameters.AddWithValue("@Image1Path", string.IsNullOrEmpty(emergency.Image1Path) ? (object)DBNull.Value : emergency.Image1Path);
                    command.Parameters.AddWithValue("@Image2Path", string.IsNullOrEmpty(emergency.Image2Path) ? (object)DBNull.Value : emergency.Image2Path);
                    command.Parameters.AddWithValue("@VideoPath", string.IsNullOrEmpty(emergency.VideoPath) ? (object)DBNull.Value : emergency.VideoPath);
                    command.Parameters.AddWithValue("@AudioPath", string.IsNullOrEmpty(emergency.AudioPath) ? (object)DBNull.Value : emergency.AudioPath);

                    emergency.ID = Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        private List<Emergency> GetEmergencyList()
        {
            List<Emergency> emergencies = new List<Emergency>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ID,ROW_NUMBER() OVER (ORDER BY id desc) AS SrNo, CoverNo, PilgrimName, LastName, Address, Latitude, Longitude, Remarks, CreatedDate,Image1Path, Image2Path, VideoPath, AudioPath FROM tblEmergency where IsResolved=0 ORDER BY CreatedDate DESC";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            emergencies.Add(new Emergency
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                SrNo = Convert.ToInt32(reader["SrNo"]),
                                CoverNo = reader["CoverNo"].ToString(),
                                PilgrimName = reader["PilgrimName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Address = reader["Address"].ToString(),
                                Latitude = reader["Latitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Latitude"]),
                                Longitude = reader["Longitude"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Longitude"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                Remarks= reader["Remarks"].ToString(),
                                Image1Path= reader["Image1Path"].ToString(),
                                Image2Path = reader["Image2Path"].ToString(),
                                VideoPath = reader["VideoPath"].ToString(),
                                AudioPath = reader["AudioPath"].ToString()
                            });
                        }
                    }
                }
            }
            return emergencies;
        }

        [HttpPost]
        public ActionResult ResolveEmergency(int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE tblEmergency SET IsResolved = 1 WHERE ID = @ID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ID", id);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("EmergencyList");
        }
    }
}