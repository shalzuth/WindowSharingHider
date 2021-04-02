using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace WindowSharingHider
{
    public partial class MainWindow : Form
    {
        public class WindowInfo
        {
            public String Title { get; set; }
            public IntPtr Handle { get; set; }
            public Boolean stillExists = false;
            public override string ToString()
            {
                return Title;
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            var timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        Boolean flagToPreserveSettings = false;
        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach(WindowInfo window in windowListCheckBox.Items) window.stillExists = false;
            var currWindows = WindowHandler.GetVisibleWindows();
            foreach (var window in currWindows)
            {
                var existingWindow = windowListCheckBox.Items.Cast<WindowInfo>().FirstOrDefault(i => i.Handle == window.Key);
                if (existingWindow != null)
                {
                    existingWindow.stillExists = true;
                    existingWindow.Title = window.Value;
                }
                else windowListCheckBox.Items.Add(new WindowInfo { Title = window.Value, Handle = window.Key, stillExists = true });
            }
            foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray()) if (window.stillExists == false) windowListCheckBox.Items.Remove(window);
            foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray())
            {
                var status = WindowHandler.GetWindowDisplayAffinity(window.Handle) > 0;
                var target = windowListCheckBox.GetItemChecked(windowListCheckBox.Items.IndexOf(window));
                if (target != status && flagToPreserveSettings)
                {
                    WindowHandler.SetWindowDisplayAffinity(window.Handle, target ? 0x11 : 0x0);
                    status = WindowHandler.GetWindowDisplayAffinity(window.Handle) > 0;
                }
                windowListCheckBox.SetItemChecked(windowListCheckBox.Items.IndexOf(window), status);
            }
            flagToPreserveSettings = true;
        }
    }
}
