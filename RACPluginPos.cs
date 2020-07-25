using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;
using log4net;
using GMap.NET.WindowsForms.Markers;
using MissionPlanner.Attributes;
using System.Reflection;
using MissionPlanner.Controls;
using System.ComponentModel;
using MissionPlanner.Utilities;
using System.Collections.Generic;
using System.IO;


namespace MissionPlanner.RACPluginPos
{


    public class RACPluginPos : MissionPlanner.Plugin.Plugin
    {

        private static readonly ILog log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        SplitContainer FDRightSide;
        Label lDebugInfo;
        Label lPullForce;

        //PointLatLngAlt 

        //List of pole base points, tag
        List<PointLatLngAlt> poles = new List<PointLatLngAlt>();


        internal TableLayoutPanel tlp;

        public override string Name
        {
            get { return "RACPluginPos"; }
        }

        public override string Version
        {
            get { return "0.1"; }
        }

        public override string Author
        {
            get { return "Andras Schaffer / RotorsAndCams"; }
        }

        public override bool Init()
        {

            //DisplayTextAttribute dta = typeof(CurrentState).GetProperty("ext1").GetCustomAttributes(false).OfType<DisplayTextAttribute>().ToArray()[0];

            //dta.Text = "Miafene";
            
            loopratehz = 2;

            tlp = Host.MainForm.FlightData.Controls.Find("tableLayoutPanelQuick", true).FirstOrDefault() as TableLayoutPanel;

            //Since the controls on FlighData are located in a different thread, we must use BeginInvoke to access them.
            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {

                //SplitContainer1 is hosting panel1 and panel2 where panel2 contains the map and all other controls on the map (WindDir, gps labels, zoom, joystick, etc.)
                FDRightSide = Host.MainForm.FlightData.Controls.Find("splitContainer1", true).FirstOrDefault() as SplitContainer;

                System.Windows.Forms.ToolStripMenuItem men = new System.Windows.Forms.ToolStripMenuItem() { Text = "IFP Settings" };
                men.Click += settings_Click;
                Host.FDMenuMap.Items.Add(men);

            }));


            //Set up parameters;
//            telemetryOutAddress = Host.config["IFP_ADAMS_IP", "127.0.0.1"];
//            Host.config["IFP_ADAMS_IP"] = telemetryOutAddress;

            return true;
        }


        public override bool Loaded()
        {
            return true;
        }

        public override bool Loop()
        {
            //Put together a packet and send it to the remote

            return true;
        }

        public override bool Exit()
        {
            return true;
        }


        void settings_Click(object sender, EventArgs e)
        {

            loadPoles();







            /*
            using (Form settings = new MissionPlanner.RACPluginPos.Settings(this))
            {
                MissionPlanner.Utilities.ThemeManager.ApplyThemeTo(settings);
                settings.ShowDialog();
            }
            */
        }

        //Open points file and load up the poles List with the coordinates and altitude, add tag1 as the id

        void loadPoles()
        {

            string filename;


            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.DefaultExt = "csv";
            openFileDialog1.Filter = "csv files (*.csv)|*.csv";
            openFileDialog1.FilterIndex = 2;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
            }
            else
            {
                return;
            }


            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (values.Length != 4) throw new InvalidDataException("Invalid poles data file format");


                    PointLatLngAlt p = new PointLatLngAlt();
                    p.Tag = values[0].ToString();
                    p.Lat = Convert.ToDouble(values[1]);
                    p.Lng = Convert.ToDouble(values[2]);
                    p.Alt = Convert.ToDouble(values[3]);

                    poles.Add(p);
                }
            }

            Console.WriteLine("Hello !");





        }


    }
}

