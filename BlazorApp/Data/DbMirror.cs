using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.Util;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using System.Drawing;
using BravoCentral.Pages.WHO5;
using System.Text;
using System.Globalization;

namespace BravoCentral.Data
{
    public class DbMirror
    {
        public static DbMirror main { get; protected set; } = new DbMirror();

        public bool Initialized = false;
        public User currentUser => currentSettings.currentUser;
        public Settings currentSettings;
        public StringBuilder log;


        public List<Commit> commits = new List<Commit>();
        public List<WhoQuestions> whoquestions = new List<WhoQuestions>();
        public List<ProductivityQuestions> pquestions = new List<ProductivityQuestions>();
        public List<User> users = new List<User>();
        public List<Week> listWeeks = new List<Week>();
        public List<String> listQuestionText = new List<String>();
        public List<String> listProductivityQuestionText = new List<String>();

        public Action changedUser;
        public Action initalizedAction;
        public string currentRunningDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public string serveruri = "http://localhost:8080";
        private static string applicationFolder = @"%AppData%\BravoCentral";
        private static string settingsFilePath = @"%AppData%\BravoCentral\Settings.json";

        public static async void Initialize()
        {
            Utils.Log("Db mirror init");
            main = new DbMirror();
            main.commits = new List<Commit>();

            Utils.Log(Environment.ExpandEnvironmentVariables(applicationFolder));
            if (!Directory.Exists(Environment.ExpandEnvironmentVariables(applicationFolder)))
            {
                Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(applicationFolder));
            }
            if (!File.Exists(Environment.ExpandEnvironmentVariables(settingsFilePath)))
            {
                main.currentSettings = new Settings(new User("temp", "temp"));
                SaveSettings();
            }
            else
            {
                main.currentSettings = Utils.FromJsonFile<Settings>(Environment.ExpandEnvironmentVariables(settingsFilePath));
            }

            Utils.Log(main.currentRunningDirectory);
            main.log = new StringBuilder();
            main.log.AppendLine($"STARTING LOG {DateTime.Now.ToShortTimeString()}");
            Utils.Log("SERVER URI: " + main.serveruri);

            main.listWeeks = Utils.FromJsonFile<List<Week>>($"{main.currentRunningDirectory}/Data/Files/Weeks.json");
            main.users = Utils.FromJsonFile<List<User>>($"{main.currentRunningDirectory}/Data/Files/Users.json");
            main.listQuestionText = Utils.FromJsonFile<List<String>>($"{main.currentRunningDirectory}/Data/Files/WHO5Questions.json");
            main.listProductivityQuestionText = Utils.FromJsonFile<List<String>>($"{main.currentRunningDirectory}/Data/Files/ProductivityQuestions.json");

            for (int i = 0; i < main.listQuestionText.Count; i++)
            {
                Utils.Log(main.listQuestionText[i]);
            }
            //
            for (int i = 0; i < main.listProductivityQuestionText.Count; i++)
            {
                Utils.Log(main.listProductivityQuestionText[i]);
            }

            await main.GetWhoQuestionsFromDB();
            await main.GetProductivityQuestionsFromDB();
            await main.GetCommitsFromDB();

            main.Initialized = true;
            main.initalizedAction.Invoke();
        }

        public bool SelectUser(string email)
        {
            foreach (User user in users)
            {
                if (user.email == email)
                {
                    main.currentSettings.currentUser = user;
                    SaveSettings();
                    changedUser?.Invoke();
                    return true;
                }
            }
            return false;
        }

        static void SaveSettings()
        {
            File.WriteAllText(Environment.ExpandEnvironmentVariables(settingsFilePath), Utils.ToJsonString(main.currentSettings));
        }


        public async Task GetWhoQuestionsFromDB()
        {
            var http = new HttpClient();
            try
            {
                HttpResponseMessage res = await http.GetAsync($"{serveruri}/db/who5/all");
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string jsonRes = await res.Content.ReadAsStringAsync();
                    Utils.Log("/db/who5/all");
                    if (DbMirror.main == null)
                    {
                        Utils.Log("main null");
                    }
                    if (DbMirror.main.whoquestions == null)
                    {
                        Utils.Log("main null");
                    }
                    DbMirror.main.whoquestions = JsonConvert.DeserializeObject<List<WhoQuestions>>(jsonRes);
                    DbMirror.main.whoquestions = DbMirror.main.whoquestions.OrderBy(question => question.week.dayStart).ToList();
                }
                else
                {
                    DbMirror.main.whoquestions = new();
                }
            }
            catch (HttpRequestException e)
            {
                Utils.Log("\nException Caught!");
                Utils.Log($"Message :{e.Message} ");
            }
        }

        public async Task GetProductivityQuestionsFromDB()
        {
            var http = new HttpClient();
            try
            {
                HttpResponseMessage res = await http.GetAsync($"{serveruri}/db/productivity/all");
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string jsonRes = await res.Content.ReadAsStringAsync();
                    Utils.Log("/db/productivity/all");
                    if (DbMirror.main == null)
                    {
                        Utils.Log("main null");
                    }
                    if (DbMirror.main.pquestions == null)
                    {
                        Utils.Log("main null");
                    }
                    List<ProductivityQuestions> tempList = JsonConvert.DeserializeObject<List<ProductivityQuestions>>(jsonRes);
                    if (DbMirror.main.pquestions.Count < tempList.Count)
                    {
                        DbMirror.main.pquestions = tempList.OrderBy(question => question.week.dayStart).ToList();
                    }
                }
                else
                {
                    DbMirror.main.pquestions = new();
                }
            }
            catch (HttpRequestException e)
            {
                Utils.Log("\nException Caught!");
                Utils.Log($"Message :{e.Message} ");
            }
        }


        public async Task GetUsersFromDB()
        {
            var http = new HttpClient();
            try
            {
                HttpResponseMessage res = await http.GetAsync($"{serveruri}/db/users/all");
                string jsonRes = await res.Content.ReadAsStringAsync();
                Utils.Log("/db/users/all");
                if (DbMirror.main == null)
                {
                    Utils.Log("main null");
                }
                if (DbMirror.main.users == null)
                {
                    Utils.Log("main null");
                }
                DbMirror.main.users = JsonConvert.DeserializeObject<List<User>>(jsonRes);
            }
            catch (HttpRequestException e)
            {
                Utils.Log("\nException Caught!");
                Utils.Log($"Message :{e.Message} ");
            }
        }

        public async Task GetCommitsFromDB()
        {
            var http = new HttpClient();
            try
            {
                HttpResponseMessage res = await http.GetAsync($"{serveruri}/db/commits/all");
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string jsonRes = await res.Content.ReadAsStringAsync();
                    Utils.Log("db/commits/all");
                    var settings = new JsonSerializerSettings
                    {
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                        DateFormatHandling = DateFormatHandling.IsoDateFormat
                    };
                    List<Commit> tempList = JsonConvert.DeserializeObject<List<Commit>>(jsonRes, settings);
                    if (commits.Count < tempList.Count)
                    {
                        commits = tempList.OrderBy(commit => commit.CommitedDate).ToList();
                    }
                }
                else
                {
                    commits = new();
                }

                Utils.Log($"Commit count {DbMirror.main.commits.Count}");
            }
            catch (HttpRequestException e)
            {
                Utils.Log("\nException Caught!");
                Utils.Log($"Message :{e.Message} ");
            }
        }

        public int CountCommitsWeek(int week)
        {
            int sum = 0;
            Week choosenWeek = listWeeks[week];
            for (int i = 0; i < commits.Count; i++)
            {
                Commit commit = commits[i];
                if (commit.CommitedDate.IsBetween(choosenWeek.dayStart, choosenWeek.dayEnd))
                {
                    sum += 1;
                }
            }
            return sum;
        }

        public int CountCommitsWeekPerUser(int week, User user)
        {
            int sum = 0;
            Week choosenWeek = listWeeks[week];
            for (int i = 0; i < commits.Count; i++)
            {
                Commit commit = commits[i];
                if (commit.CommiterEmail == user.email)
                {
                    if (commit.CommitedDate.IsBetween(choosenWeek.dayStart, choosenWeek.dayEnd))
                    {
                        sum += 1;
                    }
                }
            }
            return sum;
        }

        public int CommitAlterationsPerWeekPerUser(int week, User user)
        {
            int sum = 0;
            Week choosenWeek = listWeeks[week];
            for (int i = 0; i < commits.Count; i++)
            {
                Commit commit = commits[i];
                if (commit.CommiterEmail == user.email)
                {
                    if (commit.ContainsCodeChanges)
                    {
                        if (commit.CommitedDate.IsBetween(choosenWeek.dayStart, choosenWeek.dayEnd))
                        {
                            sum += commit.CodeStats.Total;
                        }
                    }
                }
            }

            return sum;
        }

        public int CommitAdditionsPerWeekPerUser(int week, User user)
        {
            int sum = 0;
            Week choosenWeek = listWeeks[week];
            for (int i = 0; i < commits.Count; i++)
            {
                Commit commit = commits[i];
                if (commit.CommiterEmail == user.email)
                {
                    if (commit.ContainsCodeChanges)
                    {
                        if (commit.CommitedDate.IsBetween(choosenWeek.dayStart, choosenWeek.dayEnd))
                        {
                            sum += commit.CodeStats.Additions;
                        }
                    }
                }
            }

            return sum;
        }

        public int CommitDeletionsPerWeekPerUser(int week, User user)
        {
            int sum = 0;
            Week choosenWeek = listWeeks[week];
            for (int i = 0; i < commits.Count; i++)
            {
                Commit commit = commits[i];
                if (commit.CommiterEmail == user.email)
                {
                    if (commit.ContainsCodeChanges)
                    {
                        if (commit.CommitedDate.IsBetween(choosenWeek.dayStart, choosenWeek.dayEnd))
                        {
                            sum += commit.CodeStats.Deletions;
                        }
                    }
                }
            }

            return sum;
        }


        public BarDataset<int> CreateCommitAlterationsPerWeekPerUser(User user)
        {
            BarDataset<int> dataset = new BarDataset<int>()
            {
                Label = $"Qtd Alterações: {user.ShortName}",
                BorderColor = ColorUtil.FromDrawingColor(user.colorPrimary),
                BackgroundColor = ColorUtil.FromDrawingColor(user.colorSecundary),
            };


            for (int i = 0; i < listWeeks.Count; i++)
            {
                dataset.Add(CommitAlterationsPerWeekPerUser(i, user));
            }

            return dataset;
        }

        public BarDataset<int> CreateCommitAddsPerWeekPerUser(User user)
        {
            BarDataset<int> dataset = new BarDataset<int>()
            {
                Label = $"Qtd Adições: {user.ShortName}",
                BorderColor = ColorUtil.FromDrawingColor(user.colorPrimary),
                BackgroundColor = ColorUtil.FromDrawingColor(user.colorSecundary),
            };


            for (int i = 0; i < listWeeks.Count; i++)
            {
                dataset.Add(CommitAdditionsPerWeekPerUser(i, user));
            }

            return dataset;
        }

        public static async void ForceUpdateDB()
        {
            await main.GetWhoQuestionsFromDB();
            await main.GetProductivityQuestionsFromDB();
            await main.GetCommitsFromDB();
        }

        public BarDataset<int> CreateCommitDeletionsPerWeekPerUser(User user)
        {
            BarDataset<int> dataset = new BarDataset<int>()
            {
                Label = $"Qtd Deleções: {user.ShortName}",
                BorderColor = ColorUtil.FromDrawingColor(user.colorPrimary),
                BackgroundColor = ColorUtil.FromDrawingColor(user.colorSecundary),
            };


            for (int i = 0; i < listWeeks.Count; i++)
            {
                dataset.Add(CommitDeletionsPerWeekPerUser(i, user));
            }

            return dataset;
        }

        public BarConfig GetDefaultBarConfig()
        {
            return new BarConfig
            {
                Options = new BarOptions
                {
                    Responsive = true, //TODO Change this to change the size of the chart once it's done
                    Legend = new Legend
                    {
                        Display = true,
                        Position = Position.Right
                    },
                    Title = new OptionsTitle
                    {
                        Display = false,
                    },
                    Scales = new BarScales
                    {
                        YAxes = new List<CartesianAxis>
                        {
                            new LinearCartesianAxis
                            {
                                Ticks = new LinearCartesianTicks
                                    {
                                        BeginAtZero = true,
                                    }
                            }
                        },
                        XAxes = new List<CartesianAxis>
                        {
                            new CategoryAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = "Week"
                                }
                            }
                        }
                    }
                }
            };
        }

        public string GetNameByEmail(string email) => users.FirstOrDefault(x => x.email == email).displayName;

        public void CreateWeeksFile(int year)
        {
            DateTime startDate = GetFirstMondayOfYear(year);

            List<Week> weeks = new List<Week>();
            DateTime endDate = startDate.AddDays(6); // Get the first week's end date

            int weekNumber = 1;
            while (startDate.Year == year)
            {
                Week newWeek = new Week($"Week {weekNumber} {startDate.ToString("MMMM", CultureInfo.InvariantCulture)}", startDate, endDate);
                weeks.Add(newWeek);

                startDate = endDate.AddDays(1);
                endDate = startDate.AddDays(6);
                weekNumber++;
            }

            string json = JsonConvert.SerializeObject(weeks, Formatting.Indented);
            string fullPath = Environment.ExpandEnvironmentVariables(applicationFolder) + "\\weeks.json";
            Utils.Log($"Creating weeks.json file in [{fullPath}]");

            File.WriteAllText(fullPath, json);
        }

        static DateTime GetFirstMondayOfYear(int year)
        {
            DateTime date = new DateTime(year, 1, 1);

            int daysUntilMonday = DayOfWeek.Monday - date.DayOfWeek;
            if (daysUntilMonday < 0)
                daysUntilMonday += 7; // Move to the next Monday
            return date.AddDays(daysUntilMonday);
        }
    }
}