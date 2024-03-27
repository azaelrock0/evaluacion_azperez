using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using API_REST_evaluacion_aperez.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Configuration;

namespace evaluacion_azperez
{
    public partial class servicio_azperez : ServiceBase
    {
        Timer timer = new Timer();
        public servicio_azperez()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer.Elapsed += new ElapsedEventHandler(leerFCT);
            timer.Interval = 60000;
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            timer.Enabled = false;
            timer.Stop();
        }

        private void leerFCT(object source, ElapsedEventArgs e)
        {
            string pendientesPath = ConfigurationManager.AppSettings["pendientesPath"];
            string procesadosPath = ConfigurationManager.AppSettings["procesadosPath"];
            string logPath = ConfigurationManager.AppSettings["logPath"];
            if (Directory.Exists(pendientesPath))
            {
                try
                {
                    char[] toTrim = { '|', '-', '>' };
                    Regex reg = new Regex(@".*\.fct$");
                    List<string> files = Directory.EnumerateFiles(pendientesPath, "*.fct").Where(path => reg.IsMatch(path)).ToList();
                    foreach (string file in files)
                    {
                        string s = File.ReadAllText(file);
                        s = Regex.Replace(s, @"\s+", "").Trim(toTrim);
                        string[] fields = s.Split(new char[] { '|' });
                        TicketSP ticket = new TicketSP()
                        {
                            IdTienda = fields[0],
                            IdRegistradora = fields[1],
                            Fecha = fields[2],
                            Hora = fields[3],
                            Ticket = fields[4],
                            Impuesto = fields[5],
                            Total = fields[6]
                        };
                        var handler = new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = delegate { return true; },
                        };
                        using (var client = new HttpClient(handler))
                        {
                            string url = "https://localhost:7000/api/Ticket";
                            client.DefaultRequestHeaders.Clear();
                            string jsonString = JsonConvert.SerializeObject(ticket);
                            var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                            var response = client.PostAsync(url, httpContent).Result;

                            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                            {
                                if (!File.Exists(file + "_error"))
                                    File.Move(file, file + "_error");
                                else
                                    File.Delete(file);

                                if (!Directory.Exists(logPath))
                                    Directory.CreateDirectory(logPath);
                                if (!File.Exists(logPath + "Log.txt"))
                                    File.WriteAllText(logPath + "Log.txt", "Error al insertar el archivo " + Path.GetFileName(file) + " en la base de datos.\n");
                                else
                                    File.AppendAllText(logPath + "Log.txt", "Error al insertar el archivo " + Path.GetFileName(file) + " en la base de datos.\n");
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                if (!Directory.Exists(procesadosPath))
                                    Directory.CreateDirectory(procesadosPath);

                                if (!File.Exists(procesadosPath + Path.GetFileName(file)))
                                    File.Move(file, procesadosPath + Path.GetFileName(file));
                                else
                                    File.Delete(file);

                                if (!Directory.Exists(logPath))
                                    Directory.CreateDirectory(logPath);
                                if (!File.Exists(logPath + "Log.txt"))
                                    File.WriteAllText(logPath + "Log.txt", Path.GetFileName(file) + " insertado con éxito.\n");
                                else
                                    File.AppendAllText(logPath + "Log.txt", Path.GetFileName(file) + " insertado con éxito.\n");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!Directory.Exists(logPath))
                        Directory.CreateDirectory(logPath);
                    if (!File.Exists(logPath + "Log.txt"))
                        File.WriteAllText(logPath + "Log.txt", "Error al conectarse con la API.\n");
                    else
                        File.AppendAllText(logPath + "Log.txt", "Error al conectarse con la API.\n");
                }
            }
        }
    }
}
