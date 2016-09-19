using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Data;
using System.Globalization;



namespace SCHOOL_LOAD
{
    class Program
    {
        private static readonly Regex d_ncesid = new Regex(@"^12\d{5}$");

        public static bool verifyID (string id_District)
        {
            return d_ncesid.IsMatch(id_District);
        }


        static void Main(string[] args)
        {
            using (
                var conn = new SqlConnection("Server=0;Database=0;User ID=0;Password= 0;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;")
                )
            {
                conn.Open();
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                string msidQuery = "SELECT * FROM MSID WHERE ACTIVITY_CODE = 'A'";
                SqlCommand mcmd = new SqlCommand(msidQuery, conn);
                SqlDataAdapter msid_da = new SqlDataAdapter(mcmd);
                DataTable msid = new DataTable();
                msid_da.Fill(msid);

                string districtQuery = "SELECT leaID, county FROM District";
                SqlCommand dcmd = new SqlCommand(districtQuery, conn);
                SqlDataAdapter district_da = new SqlDataAdapter(dcmd);
                DataTable district = new DataTable();
                district_da.Fill(district);

                string gcQuery = "SELECT * FROM GradeCode";
                SqlCommand gcmd = new SqlCommand(gcQuery, conn);
                SqlDataAdapter gc_da = new SqlDataAdapter(gcmd);
                DataTable gc = new DataTable();
                gc_da.Fill(gc);

                string schoolQuery = "SELECT * FROM School";
                SqlCommand scmd = new SqlCommand(schoolQuery, conn);
                SqlDataAdapter school_da = new SqlDataAdapter(scmd);
                DataTable school = new DataTable();
                school_da.Fill(school);

                // start of crazy table loops

                for (int i = 0; i < msid.Rows.Count; i++)
                {
                    school.Rows.Add();
                }

                for (int j = 0; j < school.Rows.Count; j++)
                {
                    var leaID = msid.Rows[j]["district"].ToString().PadLeft(2, '0');
                    school.Rows[j]["leaID"] = leaID;

                    var schoolID = msid.Rows[j]["school"].ToString().PadLeft(4, '0');
                    school.Rows[j]["schoolID"] = schoolID;

                    string name;
                    if (msid.Rows[j]["school_name_long"].ToString().Length >= 60)
                    {
                        name = msid.Rows[j]["school_name_long"].ToString();
                        name = textInfo.ToTitleCase(name.ToLower());
                        school.Rows[j]["name"] = name;
                    }
                    else
                    {
                        name = msid.Rows[j]["school_name_short"].ToString();
                        name = textInfo.ToTitleCase(name.ToLower());
                        school.Rows[j]["name"] = name;
                    }

                    string type;
                    switch (msid.Rows[j]["type"].ToString())
                    {
                        case "1": case "2": case "3": case "4":
                            type = "K12School";
                            school.Rows[j]["organizationType"] = type;
                            break;
                        case "5":
                            type = "Adult";
                            school.Rows[j]["organizationType"] = type;
                            break;
                        default:
                            type = "Other";
                            school.Rows[j]["organizationType"] = type;
                            break;
                    }

                    var gradeCode = msid.Rows[j]["grade_code"].ToString().PadLeft(2, '0');
                    string grades = "";

                    for (int k = 0; k < gc.Rows.Count; k++)
                    {
                        if (gc.Rows[k]["fl_grade_code"].ToString() == gradeCode)
                        {
                            grades = gc.Rows[k]["sif_grades_offered"].ToString();
                            break;
                        }
                        else
                        {
                            grades = "NULL";
                        }
                    }
                    // if grades = NULL, report error in log file
                    // Unable to find a match, gradesOffered now equals "NULL"

                    school.Rows[j]["gradesOffered"] = grades;

                    string address = msid.Rows[j]["physical_address"].ToString();
                    address = textInfo.ToTitleCase(address.ToLower());
                    school.Rows[j]["streetLine1"] = address;

                    school.Rows[j]["streetLine2"] = "NULL";

                    string city = msid.Rows[j]["physical_city"].ToString();
                    city = textInfo.ToTitleCase(city.ToLower());
                    school.Rows[j]["city"] = city;

                    string state = msid.Rows[j]["physical_state"].ToString();
                    if (state == "FL")
                    {
                        school.Rows[j]["stateProvince"] = state;
                    }
                    else
                    {
                        // report error in log file.
                        // physical_state must be of value "FL"
                    }

                    string zip = msid.Rows[j]["physical_zip"].ToString();
                    
                    var removeDash = new string[] { "-" };

                    foreach (var c in removeDash)
                        zip = zip.Replace(c, string.Empty);

                    school.Rows[j]["postalCode"] = zip;
                    

                    string county = ""; 

                    for (int m = 0; m < district.Rows.Count; m++)
                    {
                        if(district.Rows[m]["leaID"].ToString() == leaID)
                        {
                            county = district.Rows[m]["county"].ToString();
                            break;
                        }
                    }

                    school.Rows[j]["county"] = county;

                    string id_District = msid.Rows[j]["federal_dist_no"].ToString();

                    if(id_District.Length != 7)
                    {
                        //report error in log file
                        //FEDERAL_DIST_NO is not at the accepted length of 7 characters.
                    }
                    else if(verifyID(id_District))
                    {
                        school.Rows[j]["NCESID_district"] = id_District;
                    }
                    else
                    {
                        //report error in log file 
                        //FEDERAL_DIST_NO is not in accepted format "12#####"
                    }


                    string id_School = msid.Rows[j]["federal_schl_no"].ToString().PadLeft(5, '0');
                    school.Rows[j]["NCESID_school"] = id_School;

                    school.Rows[j]["code1"] = null;
                    school.Rows[j]["code2"] = null;

                    school.Rows[j]["fl_grade_code"] = msid.Rows[j]["grade_code"];
                    school.Rows[j]["fl_school_type"] = msid.Rows[j]["type"];
                    school.Rows[j]["fl_charter_school_status"] = msid.Rows[j]["charter_schl_stat"];
                    school.Rows[j]["fl_region_code"] = msid.Rows[j]["region_code"];
                        

                }

                for (int n = 0; n < school.Rows.Count; n++)
                {
                    //string com = school.Rows[n][0].ToString() + "','" + school.Rows[n][1].ToString() + "','" + school.Rows[n][2].ToString() + "','" + school.Rows[n][3].ToString() + "','" + school.Rows[n][4].ToString() + "','" + school.Rows[n][5].ToString() + "','" + school.Rows[n][6].ToString() + "','" + school.Rows[n][7].ToString() + "','" + school.Rows[n][8].ToString() + "','" + school.Rows[n][9].ToString() + "','" + school.Rows[n][10].ToString() + "','" + school.Rows[n][11].ToString() + "','" + school.Rows[n][12].ToString() + "','" + school.Rows[n][13].ToString() + "','" + school.Rows[n][14].ToString() + "','" + school.Rows[n][15].ToString() + "','" + school.Rows[n][16].ToString() + "','" + school.Rows[n][17].ToString() + "','" + school.Rows[n][18].ToString();
                    SqlCommand command = new SqlCommand("INSERT INTO School (leaID,schoolID,name,organizationType,gradesOffered,streetLine1,streetLine2,city,stateProvince,postalCode,county,NCESID_district,NCESID_school,code1,code2,FL_grade_code,FL_school_type,FL_charter_school_status,FL_region_code) VALUES(@leaID, @schoolID, @name, @organizationType, @gradesOffered, @streetLine1, @streetLine2, @city, @stateProvince, @postalCode, @county, @NCESID_district, @NCESID_school, @code1, @code2, @fl_grade_code, @fl_school_type, @fl_charter_school_status, @fl_region_code)", conn);

                    command.Parameters.AddWithValue("@leaID", school.Rows[n][0]);
                    command.Parameters.AddWithValue("@schoolID", school.Rows[n][1]);
                    command.Parameters.AddWithValue("@name", school.Rows[n][2]);
                    command.Parameters.AddWithValue("@organizationType", school.Rows[n][3]);
                    command.Parameters.AddWithValue("@gradesOffered", school.Rows[n][4]);
                    command.Parameters.AddWithValue("@streetLine1", school.Rows[n][5]);
                    command.Parameters.AddWithValue("@streetLine2", school.Rows[n][6]);
                    command.Parameters.AddWithValue("@city", school.Rows[n][7]);
                    command.Parameters.AddWithValue("@stateProvince", school.Rows[n][8]);
                    command.Parameters.AddWithValue("@postalCode", school.Rows[n][9]);
                    command.Parameters.AddWithValue("@county", school.Rows[n][10]);
                    command.Parameters.AddWithValue("@NCESID_district", school.Rows[n][11]);
                    command.Parameters.AddWithValue("@NCESID_school", school.Rows[n][12]);
                    command.Parameters.AddWithValue("@code1", school.Rows[n][13]);
                    command.Parameters.AddWithValue("@code2", school.Rows[n][14]);
                    command.Parameters.AddWithValue("@fl_grade_code", school.Rows[n][17]);
                    command.Parameters.AddWithValue("@fl_school_type", school.Rows[n][18]);
                    command.Parameters.AddWithValue("@fl_charter_school_status", school.Rows[n][19]);
                    command.Parameters.AddWithValue("@fl_region_code", school.Rows[n][20]);
                    command.ExecuteNonQuery();
                }

                conn.Close();
                msid_da.Dispose();
                district_da.Dispose();
                gc_da.Dispose();
                school_da.Dispose();
            }
        }



    }
}
