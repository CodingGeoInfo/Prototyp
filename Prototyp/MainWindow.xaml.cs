﻿using DynamicData;
using Microsoft.Win32;
using NodeNetwork.ViewModels;
using Prototyp.Elements;
using Prototyp.Modules;
using System;
using System.IO;
using System.Windows;

namespace Prototyp
{
    public partial class MainWindow : Window
    {
        public System.Collections.Generic.List<VectorData> vectorData = new System.Collections.Generic.List<VectorData>();
        public System.Collections.Generic.List<RasterData> rasterData = new System.Collections.Generic.List<RasterData>();

        private string ModulesPath;

        public static MainWindow AppWindow;
        NetworkViewModel network = new NetworkViewModel();

        // Constructors --------------------------------------------------------------------

        // Parameterless constructor: Create a new viewmodel for the NetworkView.
        public MainWindow()
        {
            ModulesPath = "..\\..\\..\\..\\Custom modules";
            ParseModules();

            InitializeComponent();
            AppWindow = this;
            networkView.ViewModel = network;
        }

        // Static methods --------------------------------------------------------------------

        public void ParseModules()
        {
            
            MessageBox.Show(ModulesPath);
        }

        // Private methods --------------------------------------------------------------------

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "FlatGeobuf files (*.fgb)|*.fgb|" +
                                    "Shapefiles (*.shp)|*.shp|" +
                                    "GeoTiff (*.tif)|*.tif|" +
                                    "GeoASCII (*.asc)|*.asc|" +
                                    "Raster files (*.sdat)|*.sdat|" +
                                    "All files (*.*)|*.*";
            openFileDialog.FilterIndex = openFileDialog.Filter.Length;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Title = "Open a data file...";

            Nullable<bool> result = openFileDialog.ShowDialog();
            if (result == true)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".shp" | Path.GetExtension(openFileDialog.FileName).ToLower() == ".geojson") //TODO: Auch andere GDAL-Layer-Dateitypen möglich.
                {
                    if (vectorData.Count > 0)
                    {
                        if (vectorData[vectorData.Count - 1] != null)
                        {
                            while (vectorData[vectorData.Count - 1].Busy)
                            {
                                System.Threading.Thread.Sleep(50);
                            }
                        }
                    }

                    VectorData.InitGDAL();
                    OSGeo.OGR.DataSource MyDS;
                    MyDS = OSGeo.OGR.Ogr.Open(openFileDialog.FileName, 0);

                    if (MyDS != null)
                    {
                        vectorData.Add(new VectorData(MyDS.GetLayerByIndex(0)));
                        MainWindowHelpers mainWindowHelpers = new MainWindowHelpers();
                        mainWindowHelpers.AddTreeViewChild(vectorData[vectorData.Count - 1]);
                    }
                }
                else if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".fgb")
                {
                    if (vectorData.Count > 0)
                    {
                        if (vectorData[vectorData.Count - 1] != null)
                        {
                            while (vectorData[vectorData.Count - 1].Busy)
                            {
                                System.Threading.Thread.Sleep(50);
                            }
                        }
                    }

                    vectorData.Add(new VectorData(openFileDialog.FileName));
                    MainWindowHelpers mainWindowHelpers = new MainWindowHelpers();
                    mainWindowHelpers.AddTreeViewChild(vectorData[vectorData.Count - 1]);
                }
                else if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".tif" | Path.GetExtension(openFileDialog.FileName).ToLower() == ".asc" | Path.GetExtension(openFileDialog.FileName).ToLower() == ".sdat") //TODO: Auch andere GDAL-Raster-Dateitypen möglich.
                {
                    if (rasterData.Count > 0)
                    {
                        if (rasterData[rasterData.Count - 1] != null)
                        {
                            while (rasterData[rasterData.Count - 1].Busy)
                            {
                                System.Threading.Thread.Sleep(50);
                            }
                        }
                    }

                    rasterData.Add(new RasterData(openFileDialog.FileName));
                    MainWindowHelpers mainWindowHelpers = new MainWindowHelpers();
                    mainWindowHelpers.AddTreeViewChild(rasterData[rasterData.Count - 1]);
                }
                //else if (...) //TODO: Ggf. andere Datentypen...
                //{

                //}
                else
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                    return;
                }

                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            //Find lowest available node ID
            int port = 5001;
            foreach(Node_Module node in network.Nodes.Items)
            {
                if(node.port == port)
                {
                    port++;
                    //TODO: Check if port is open https://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
                }
            }
            if (!Node_Module.PortAvailable(port)) throw new System.Exception("This port is not available."); //TODO: Besseres Handling. Nächsten Kandidaten holen?


            GrpcClient.ControlConnector.ControlConnectorClient grpcConnection;
            using (System.Diagnostics.Process myProcess = new System.Diagnostics.Process())
            {
                //Start binary
                //TODO: Pfad zur Binary aus der XML holen
                string path = "";
                System.Diagnostics.ProcessStartInfo myProcessStartInfo = new System.Diagnostics.ProcessStartInfo(
                    "..\\..\\..\\..\\Modules\\Buffer\\Buffer.exe", port.ToString());

                myProcessStartInfo.UseShellExecute = true;
                myProcess.StartInfo = myProcessStartInfo;
                myProcess.Start();

                //Establish GRPC connection
                //TODO: nicht nur localhost
                var url = "https://localhost:" + port;
                var channel = Grpc.Net.Client.GrpcChannel.ForAddress(url);
                grpcConnection = new GrpcClient.ControlConnector.ControlConnectorClient(channel);
            }

            var nodeModule = new Node_Module("..\\..\\..\\..\\Modules\\Buffer\\Buffer.xml", port, grpcConnection);

            network.Nodes.Add(nodeModule);

            //TODO: Das sollte eigentlich bereits beim Programmstart durchlaufen werden, dann auf Basis aller installierten Module.
            ToolButton1.Text = nodeModule.Name;
            Button1.ToolTip = nodeModule.Name;
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hallo");
        }

        //Objekt wird in den NodeEditor gezogen
        public void DropTargetEventNodeEditor(object sender, DragEventArgs e)
        {
            if ((string)e.Data.GetData("Type") == "Vector")
            {
                VectorImport_Module importNode = new VectorImport_Module();

                for (int i = 0; i < vectorData.Count; i++)
                {
                    if (vectorData[i].ID.ToString() == (string)e.Data.GetData("ID"))
                    {
                        importNode.importNodeOutput.Name = vectorData[i].Name;
                        importNode.importNodeOutput.Value = System.Reactive.Linq.Observable.Return(vectorData[i]);
                        importNode.Position = e.GetPosition(networkView);
                        network.Nodes.Add(importNode);
                        break;
                    }
                }
            }
            else if ((string)e.Data.GetData("Type") == "Raster")
            {
                RasterImport_Module importNode = new RasterImport_Module();

                for (int i = 0; i < rasterData.Count; i++)
                {
                    if (rasterData[i].ID.ToString() == (string)e.Data.GetData("ID"))
                    {
                        importNode.importNodeOutput.Name = rasterData[i].Name;
                        importNode.importNodeOutput.Value = System.Reactive.Linq.Observable.Return(rasterData[i]);
                        importNode.Position = e.GetPosition(networkView);
                        network.Nodes.Add(importNode);
                        break;
                    }
                }
            }
        }
    }
}

