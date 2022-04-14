﻿using DynamicData;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using Prototyp.Elements;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Windows;

namespace Prototyp.Modules
{
    //Property extensions, see https://stackoverflow.com/questions/17616239/c-sharp-extend-class-by-adding-properties

    // Extend OUTput
    public static class NodeOutputViewModelExtension
    {
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<NodeOutputViewModel, IntObject> IDs = new System.Runtime.CompilerServices.ConditionalWeakTable<NodeOutputViewModel, IntObject>();
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<NodeOutputViewModel, DoubleObject> DataIDs = new System.Runtime.CompilerServices.ConditionalWeakTable<NodeOutputViewModel, DoubleObject>();
        public static int GetID(this NodeOutputViewModel ID) { return IDs.GetOrCreateValue(ID).iValue; }
        public static void SetID(this NodeOutputViewModel ID, int newID) { IDs.GetOrCreateValue(ID).iValue = newID; }
        public static double GetDataID(this NodeOutputViewModel DataID) { return DataIDs.GetOrCreateValue(DataID).dValue; }
        public static void SetDataID(this NodeOutputViewModel DataID, double newDataID) { DataIDs.GetOrCreateValue(DataID).dValue = newDataID; }

        class IntObject
        {
            public int iValue;
        }
        class DoubleObject
        {
            public double dValue;
        }
    }

    // Extend INput
    public static class NodeInputViewModelExtension
    {
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<NodeInputViewModel, IntObject> IDs = new System.Runtime.CompilerServices.ConditionalWeakTable<NodeInputViewModel, IntObject>();
        public static int GetID(this NodeInputViewModel ID) { return IDs.GetOrCreateValue(ID).Value; }
        public static void SetID(this NodeInputViewModel ID, int newID) { IDs.GetOrCreateValue(ID).Value = newID; }

        class IntObject
        {
            public int Value;
        }
    }

    public class Node_Module : NodeViewModel
    {
        public event EventHandler ProcessStatusChanged;
        public Modules.ViewModels.FloatSliderViewModel sliderEditor { get; set; }
        public Modules.ViewModels.OutputNameViewModel outNameEditor { get; set; }
        public Modules.ViewModels.DropDownMenuViewModel dropDownEditor { get; set; }
        public ValueNodeInputViewModel<Prototyp.Elements.VectorPointData> vectorInputPoint { get; set; }
        public ValueNodeInputViewModel<Prototyp.Elements.VectorLineData> vectorInputLine { get; set; }
        public ValueNodeInputViewModel<Prototyp.Elements.VectorPolygonData> vectorInputPolygon { get; set; }
        public ValueNodeInputViewModel<Prototyp.Elements.VectorMultiPolygonData> vectorInputMultiPolygon { get; set; }
        public ValueNodeInputViewModel<Prototyp.Elements.RasterData> rasterInput { get; set; }
        public ValueNodeInputViewModel<float> valueFloatInput { get; set; }
        public ValueNodeInputViewModel<string> valueStringInput { get; set; }
        public ValueNodeOutputViewModel<Prototyp.Elements.VectorPointData> vectorOutputPoint { get; set; }
        public ValueNodeOutputViewModel<Prototyp.Elements.VectorLineData> vectorOutputLine { get; set; }
        public ValueNodeOutputViewModel<Prototyp.Elements.VectorPolygonData> vectorOutputPolygon { get; set; }
        public ValueNodeOutputViewModel<Prototyp.Elements.VectorMultiPolygonData> vectorOutputMultiPolygon { get; set; }
        public ValueNodeOutputViewModel<Prototyp.Elements.RasterData> rasterOutput { get; set; }

        private string IntPathXML;
        private GrpcClient.ControlConnector.ControlConnectorClient IntGrpcConnection;
        private System.Diagnostics.Process IntProcess;
        private string IntUrl;
        public NodeProgress Status;

        public void ChangeStatus(NodeProgress statusNumber)
        {
            Status = statusNumber;
            ProcessStatusChanged?.Invoke(Status, EventArgs.Empty);
        }

        // Getters and setters -------------------------------------------------------------

        public string PathXML
        {
            get { return (IntPathXML); }
            set { IntPathXML = value; }
        }

        public GrpcClient.ControlConnector.ControlConnectorClient grpcConnection
        {
            get { return (IntGrpcConnection); }
            set { IntGrpcConnection = value; }
        }

        public System.Diagnostics.Process Process
        {
            get { return (IntProcess); }
        }

        public string Url
        {
            get { return (IntUrl); }
            set { IntUrl = value; }
        }

        // Constructors --------------------------------------------------------------------

        // Parameterless constructor.
        public Node_Module()
        {
            // Nothing much to do here...
        }

        // Used for actually adding something to the main window node editor.
        public Node_Module(string pathXML, GrpcClient.ControlConnector.ControlConnectorClient grpcConnection, string url, System.Diagnostics.Process process)
        {
            IntPathXML = pathXML;

            VorteXML newModule = new VorteXML(pathXML);

            Name = newModule.NodeTitle;

            IntUrl = url;
            IntProcess = process;            
            IntGrpcConnection = grpcConnection;
            
            this.Status = 0;

            ParseXML(newModule, true);
        }

        // Used for workflow loading procedure.
        public Node_Module(VorteXML XML, string Title, GrpcClient.ControlConnector.ControlConnectorClient grpcConnection, string url, System.Diagnostics.Process process)
        {
            Name = Title;

            IntUrl = url;
            IntProcess = process;
            IntGrpcConnection = grpcConnection;
            this.Status = 0;

            ParseXML(XML, true);
        }

        // Used for the module designer preview.
        public Node_Module(VorteXML newModule)
        {
            Name = newModule.NodeTitle;

            ParseXML(newModule, false);
        }

        // Private methods -----------------------------------------------------------------

        private void ParseXML(VorteXML newModule, bool inMain) //Use inMain = true for MainWindow node editor, inMain = false for ModuleDesigner preview.
        {
            foreach (VorteXML.ToolRow toolRow in newModule.ToolRows)
            {
                if (toolRow.rowType == VorteXML.RowType.Input)
                {
                    for (int i = 0; i < toolRow.inputRow.inputTypes.Length; i++)
                    {
                        if (toolRow.inputRow.inputTypes[i] == VorteXML.ConnectorType.VectorPoint)
                        {
                            vectorInputPoint = new ValueNodeInputViewModel<Prototyp.Elements.VectorPointData>();
                            if (inMain)
                            {
                                vectorInputPoint.SetID(i);
                                vectorInputPoint.Name = toolRow.inputRow.inputTypes[i].ToString();
                                // Alternativ: vectorInput.Name = toolRow.inputRow.inputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Inputtypen?
                                vectorInputPoint.ValueChanged.Subscribe(vectorInputSource =>
                                {
                                    if (vectorInputSource != null)
                                    {
                                        // TODO: Hier muss noch nach den Ports differenziert werden, falls mehrere vorhanden sind.
                                        vectorInputPoint.Name = vectorInputSource.Name;
                                    }
                                });
                            }
                            else
                            {
                                vectorInputPoint.Name = toolRow.Name;
                            }
                            Inputs.Add(vectorInputPoint);
                            break;
                        }
                        else if (toolRow.inputRow.inputTypes[i] == VorteXML.ConnectorType.VectorLine)
                        {
                            vectorInputLine = new ValueNodeInputViewModel<Prototyp.Elements.VectorLineData>();
                            if (inMain)
                            {
                                vectorInputLine.SetID(i);
                                vectorInputLine.Name = toolRow.inputRow.inputTypes[i].ToString();
                                // Alternativ: vectorInput.Name = toolRow.inputRow.inputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Inputtypen?
                                vectorInputLine.ValueChanged.Subscribe(vectorInputSource =>
                                {
                                    if (vectorInputSource != null)
                                    {
                                        // TODO: Hier muss noch nach den Ports differenziert werden, falls mehrere vorhanden sind.
                                        vectorInputLine.Name = vectorInputSource.Name;
                                    }
                                });
                            }
                            else
                            {
                                vectorInputLine.Name = toolRow.Name;
                            }
                            Inputs.Add(vectorInputLine);
                            break;
                        }
                        else if (toolRow.inputRow.inputTypes[i] == VorteXML.ConnectorType.VectorPolygon)
                        {
                            vectorInputPolygon = new ValueNodeInputViewModel<Prototyp.Elements.VectorPolygonData>();
                            if (inMain)
                            {
                                vectorInputPolygon.SetID(i);
                                vectorInputPolygon.Name = toolRow.inputRow.inputTypes[i].ToString();
                                // Alternativ: vectorInput.Name = toolRow.inputRow.inputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Inputtypen?
                                vectorInputPolygon.ValueChanged.Subscribe(vectorInputSource =>
                                {
                                    if (vectorInputSource != null)
                                    {
                                        // TODO: Hier muss noch nach den Ports differenziert werden, falls mehrere vorhanden sind.
                                        vectorInputPolygon.Name = vectorInputSource.Name;
                                    }
                                });
                            }
                            else
                            {
                                vectorInputPolygon.Name = toolRow.Name;
                            }
                            Inputs.Add(vectorInputPolygon);
                            break;
                        }
                        else if (toolRow.inputRow.inputTypes[i] == VorteXML.ConnectorType.VectorMultiPolygon)
                        {
                            vectorInputMultiPolygon = new ValueNodeInputViewModel<Prototyp.Elements.VectorMultiPolygonData>();
                            if (inMain)
                            {
                                vectorInputMultiPolygon.SetID(i);
                                vectorInputMultiPolygon.Name = toolRow.inputRow.inputTypes[i].ToString();
                                // Alternativ: vectorInput.Name = toolRow.inputRow.inputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Inputtypen?
                                vectorInputMultiPolygon.ValueChanged.Subscribe(vectorInputSource =>
                                {
                                    if (vectorInputSource != null)
                                    {
                                        // TODO: Hier muss noch nach den Ports differenziert werden, falls mehrere vorhanden sind.
                                        vectorInputMultiPolygon.Name = vectorInputSource.Name;
                                    }
                                });
                            }
                            else
                            {
                                vectorInputMultiPolygon.Name = toolRow.Name;
                            }
                            Inputs.Add(vectorInputMultiPolygon);
                            break;
                        }
                        else if (toolRow.inputRow.inputTypes[i] == VorteXML.ConnectorType.Raster)
                        {
                            rasterInput = new ValueNodeInputViewModel<Prototyp.Elements.RasterData>();
                            if (inMain)
                            {

                                rasterInput.SetID(i);
                                rasterInput.Name = toolRow.inputRow.inputTypes[i].ToString();
                                // Alternativ: rasterInput.Name = toolRow.inputRow.inputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Inputtypen?
                                rasterInput.ValueChanged.Subscribe(rasterInputSource =>
                                {
                                    if (rasterInputSource != null)
                                    {
                                        // TODO: Hier muss noch nach den Ports differenziert werden, falls mehrere vorhanden sind.
                                        rasterInput.Name = rasterInputSource.Name;
                                    }
                                });
                            }
                            else
                            {
                                rasterInput.Name = toolRow.Name;
                            }
                            Inputs.Add(rasterInput);
                            break;
                        }
                        //... TODO: Support more types?
                        else
                        {
                            throw new System.Exception("No implemented input connector type specified.");
                        }
                    }
                }
                else if (toolRow.rowType == VorteXML.RowType.Output)
                {
                    for (int i = 0; i < toolRow.outputRow.outputTypes.Length; i++)
                    {
                        if (toolRow.outputRow.outputTypes[i] == VorteXML.ConnectorType.VectorPoint)
                        {
                            vectorOutputPoint = new ValueNodeOutputViewModel<Elements.VectorPointData>();
                            if (inMain)
                            {
                                vectorOutputPoint.SetID(i);
                                VectorPointData placeholder = new VectorPointData();
                                // Name-Editor Implementation
                                //outNameEditor = new Modules.ViewModels.OutputNameViewModel(vectorOutput.Name);
                                //outNameEditor.Value = "Vector output";
                                //vectorOutput.Editor = outNameEditor;
                                //outNameEditor.ValueChanged.Subscribe (v => { result.Name = v; });
                                //vectorOutput.Value = this.WhenAnyObservable(vm => vm.outNameEditor.ValueChanged).Select(value => result);

                                // Alternativ: ...outputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Outputtypen?
                                placeholder.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputPoint.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputPoint.Value = System.Reactive.Linq.Observable.Return(placeholder);
                            }
                            else
                            {
                                vectorOutputPoint.Name = toolRow.Name;
                            }
                            Outputs.Add(vectorOutputPoint);
                            break;
                        }
                        else if (toolRow.outputRow.outputTypes[i] == VorteXML.ConnectorType.VectorLine)
                        {
                            vectorOutputLine = new ValueNodeOutputViewModel<Elements.VectorLineData>();
                            if (inMain)
                            {
                                vectorOutputLine.SetID(i);
                                VectorLineData placeholder = new VectorLineData();
                                // Name-Editor Implementation
                                //outNameEditor = new Modules.ViewModels.OutputNameViewModel(vectorOutput.Name);
                                //outNameEditor.Value = "Vector output";
                                //vectorOutput.Editor = outNameEditor;
                                //outNameEditor.ValueChanged.Subscribe (v => { result.Name = v; });
                                //vectorOutput.Value = this.WhenAnyObservable(vm => vm.outNameEditor.ValueChanged).Select(value => result);

                                // Alternativ: ...outputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Outputtypen?
                                placeholder.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputLine.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputLine.Value = System.Reactive.Linq.Observable.Return(placeholder);
                            }
                            else
                            {
                                vectorOutputLine.Name = toolRow.Name;
                            }
                            Outputs.Add(vectorOutputLine);
                            break;
                        }
                        else if (toolRow.outputRow.outputTypes[i] == VorteXML.ConnectorType.VectorPolygon)
                        {
                            vectorOutputPolygon = new ValueNodeOutputViewModel<Elements.VectorPolygonData>();
                            if (inMain)
                            {
                                vectorOutputPolygon.SetID(i);
                                VectorPolygonData placeholder = new VectorPolygonData();
                                // Name-Editor Implementation
                                //outNameEditor = new Modules.ViewModels.OutputNameViewModel(vectorOutput.Name);
                                //outNameEditor.Value = "Vector output";
                                //vectorOutput.Editor = outNameEditor;
                                //outNameEditor.ValueChanged.Subscribe (v => { result.Name = v; });
                                //vectorOutput.Value = this.WhenAnyObservable(vm => vm.outNameEditor.ValueChanged).Select(value => result);

                                // Alternativ: ...outputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Outputtypen?
                                placeholder.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputPolygon.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputPolygon.Value = System.Reactive.Linq.Observable.Return(placeholder);
                            }
                            else
                            {
                                vectorOutputPolygon.Name = toolRow.Name;
                            }
                            Outputs.Add(vectorOutputPolygon);
                            break;
                        }
                        else if (toolRow.outputRow.outputTypes[i] == VorteXML.ConnectorType.VectorMultiPolygon)
                        {
                            vectorOutputMultiPolygon = new ValueNodeOutputViewModel<Elements.VectorMultiPolygonData>();
                            if (inMain)
                            {
                                vectorOutputMultiPolygon.SetID(i);
                                VectorMultiPolygonData placeholder = new VectorMultiPolygonData();
                                // Name-Editor Implementation
                                //outNameEditor = new Modules.ViewModels.OutputNameViewModel(vectorOutput.Name);
                                //outNameEditor.Value = "Vector output";
                                //vectorOutput.Editor = outNameEditor;
                                //outNameEditor.ValueChanged.Subscribe (v => { result.Name = v; });
                                //vectorOutput.Value = this.WhenAnyObservable(vm => vm.outNameEditor.ValueChanged).Select(value => result);

                                // Alternativ: ...outputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Outputtypen?
                                placeholder.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputMultiPolygon.Name = toolRow.outputRow.outputTypes[i].ToString();
                                vectorOutputMultiPolygon.Value = System.Reactive.Linq.Observable.Return(placeholder);
                            }
                            else
                            {
                                vectorOutputMultiPolygon.Name = toolRow.Name;
                            }
                            Outputs.Add(vectorOutputMultiPolygon);
                            break;
                        }
                        else if (toolRow.outputRow.outputTypes[i] == VorteXML.ConnectorType.Raster)
                        {
                            rasterOutput = new ValueNodeOutputViewModel<Elements.RasterData>();
                            if (inMain)
                            {
                                rasterOutput.SetID(i);
                                RasterData placeholder = new RasterData();
                                // Name-Editor Implementation
                                //outNameEditor = new Modules.ViewModels.OutputNameViewModel(rasterOutput.Name);
                                //outNameEditor.Value = "Raster output";
                                //rasterOutput.Editor = outNameEditor;
                                //outNameEditor.ValueChanged.Subscribe(v => { result.Name = v; });
                                //rasterOutput.Value = this.WhenAnyObservable(vm => vm.outNameEditor.ValueChanged).Select(value => result);

                                // Alternativ: ...outputTypes.Last().ToString();
                                // Grundsätzlich: Was tun bei mehreren validen Outputtypen?
                                placeholder.Name = toolRow.outputRow.outputTypes[i].ToString();
                                rasterOutput.Name = toolRow.outputRow.outputTypes[i].ToString();
                                rasterOutput.Value = System.Reactive.Linq.Observable.Return(placeholder);
                            }
                            else
                            {
                                rasterOutput.Name = toolRow.Name;
                            }
                            Outputs.Add(rasterOutput);
                            break;
                        }
                        //... TODO: Support more types?
                        else
                        {
                            throw new System.Exception("An unimplemented output connector type was specified.");
                        }
                    }
                }
                else if (toolRow.rowType == VorteXML.RowType.Control)
                {
                    if (toolRow.controlRow.controlType == VorteXML.ControlType.Slider)
                    {
                        valueFloatInput = new ValueNodeInputViewModel<float>();
                        sliderEditor = new Modules.ViewModels.FloatSliderViewModel(toolRow.Name, toolRow.controlRow.slider.Start, toolRow.controlRow.slider.End, toolRow.controlRow.slider.TickFrequency, toolRow.controlRow.slider.Unit);
                        valueFloatInput.Editor = sliderEditor;
                        valueFloatInput.Port.IsVisible = false;
                        Inputs.Add(valueFloatInput);
                    }
                    else if (toolRow.controlRow.controlType == VorteXML.ControlType.Dropdown)
                    {
                        valueStringInput = new ValueNodeInputViewModel<string>();
                        dropDownEditor = new Modules.ViewModels.DropDownMenuViewModel(toolRow.Name, toolRow.controlRow.dropdown.Values);
                        valueStringInput.Editor = dropDownEditor;
                        valueStringInput.Port.IsVisible = false;
                        Inputs.Add(valueStringInput);
                    }
                }
            }
        }

        // Static methods ------------------------------------------------------------------

        public static bool PortAvailable(int Port) // Source: https://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
        {
            System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            System.Net.IPEndPoint[] listeners = ipGlobalProperties.GetActiveTcpListeners();
                        
            foreach (System.Net.IPEndPoint l in listeners)
            {
                if (l.Port == Port)
                {
                    return (false);
                }
            }

            return (true);
        }

        public static int GetNextPort(int StartPort)
        {
            int port = StartPort;
            while (!Node_Module.PortAvailable(port)) port++;
            if (port >= MainWindow.MAX_UNSIGNED_SHORT) throw new System.Exception("Could not find any free port.");

            return (port);
        }

        static Node_Module()
        {
            Splat.Locator.CurrentMutable.Register(() => new Views.NodeModuleView(), typeof(IViewFor<Node_Module>));
        }
    }
}
