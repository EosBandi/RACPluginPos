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

        //bearing of the power line
        double lineBearing;

        PointLatLngAlt copter_position = new PointLatLngAlt();

        //PointLatLngAlt 

        //List of pole base points, tag
        List<PointLatLngAlt> poles = new List<PointLatLngAlt>();


        internal static GMapOverlay polesLayer;
        internal GMapMarker poleMarker;



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


            polesLayer = new GMapOverlay("poles");
            Host.FDGMapControl.Overlays.Add(polesLayer);

            //Since the controls on FlighData are located in a different thread, we must use BeginInvoke to access them.
            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {

                //SplitContainer1 is hosting panel1 and panel2 where panel2 contains the map and all other controls on the map (WindDir, gps labels, zoom, joystick, etc.)
                FDRightSide = Host.MainForm.FlightData.Controls.Find("splitContainer1", true).FirstOrDefault() as SplitContainer;

                System.Windows.Forms.ToolStripMenuItem men = new System.Windows.Forms.ToolStripMenuItem() { Text = "Load pole positions" };
                men.Click += settings_Click;
                Host.FDMenuMap.Items.Add(men);


                lDebugInfo = new System.Windows.Forms.Label();
                lDebugInfo.Name = "lDebugInfo";
                //This is a good approximate position beside the wind direction and below the distance bar
                lDebugInfo.Location = new System.Drawing.Point(66, 45);
                lDebugInfo.Text = "TensionerDebugInfo";
                lDebugInfo.AutoSize = true;
                FDRightSide.Panel2.Controls.Add(lDebugInfo);
                FDRightSide.Panel2.Controls.SetChildIndex(lDebugInfo, 1);
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


            if (!Host.cs.armed) return true;

            //get actual position
            copter_position.Lat = Host.cs.lat;
            copter_position.Lng = Host.cs.lng;
            copter_position.Alt = Host.cs.altasl;

            int closest = get_closest_pole();

            PointLatLngAlt closest_pole = poles[closest];

            //No closest pole, ignore calculations
            if (closest == -1)
            {
                MainV2.instance.BeginInvoke((MethodInvoker)(() =>
                {
                    lDebugInfo.Text = "";
                }));
                return true;
            }
            
            //get line bearing
            if (closest == 0)
            {
                lineBearing = closest_pole.GetBearing(poles[closest + 1]);
            }
            else
            {
                lineBearing = poles[closest - 1].GetBearing(closest_pole);
            }

            double angle = copter_position.GetAngle(closest_pole, lineBearing);

            //angle 0-90 left before
            //angle 90-180 left after
            //angle 0- -90 right before
            //angle -180 - -90 right after




            var lineStart = closest_pole;
            var lineEnd = closest_pole;

            if (closest == 0) lineEnd = poles[closest + 1];
            else lineEnd = poles[closest - 1];


            var lineDist = lineStart.GetDistance2(lineEnd);

            var distToLocation = lineStart.GetDistance2(copter_position);
            var bearToLocation = lineStart.GetBearing(copter_position);
            var lineBear = lineStart.GetBearing(lineEnd);
            if (closest > 0) lineBear = lineBear - 180;

            var angle1 = bearToLocation - lineBear;
            if (angle1 < 0)
                angle1 += 360;

            var alongline = Math.Cos(angle1 * MathHelper.deg2rad) * distToLocation;
            var dXt2 = Math.Sin(angle * MathHelper.deg2rad) * distToLocation;


            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                lDebugInfo.Text = closest.ToString() + "   " + lineBearing.ToString() + "   " + dXt2.ToString() + "   " + alongline.ToString();
            }));



            return true;
        }

        public override bool Exit()
        {
            return true;
        }

        public int get_closest_pole()
        {
            double distance = Double.MaxValue;
            int closest_pole = -1;
            int index = 0;


            foreach (PointLatLngAlt p in poles)
            {
                double d = copter_position.GetDistance(p);
                if (d < distance)
                {
                    distance = d;
                    closest_pole = index;
                }
                index++;
            }


            return closest_pole;

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
                    p.Lat = Convert.ToDouble(values[2]);
                    p.Lng = Convert.ToDouble(values[1]);
                    p.Alt = Convert.ToDouble(values[3]);

                    poles.Add(p);
                }
            }

            Console.WriteLine("Hello !");

            foreach (PointLatLngAlt pole in poles)
            {
                GMapMarker p = new GMarkerGoogle(pole, GMarkerGoogleType.lightblue_dot);
                polesLayer.Markers.Add(p);
            }


        }


    }
}

