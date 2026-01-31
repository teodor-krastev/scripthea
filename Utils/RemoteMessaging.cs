using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace UtilsNS
{
    /// <summary>
    /// In memory meassage log - mostly for debug
    /// </summary>
    public class memLog: List<string>
    {          
        public bool Enabled = true;
        private int bufferLimit;
        public memLog(int depth = 32): base()
        {
            bufferLimit = depth;
        } 
        public void log(string txt) 
        {
            if (!Enabled) return;
            Add(txt);
            while (Count > bufferLimit) RemoveAt(0);
        }
    }

    /// <summary>
    /// Messaging service using Windows messages (quickest way possible)
    /// </summary>
    public class RemoteMessaging
    {
        public string partner { get; private set; }
        public bool silentPartner { get; set; }
        private IntPtr windowHandle;
        public string lastRcvMsg { get; private set; }
        public string lastSndMsg { get; private set; }
        public memLog Log;
        public long startTicks { get; private set; } 

        public DispatcherTimer dTimer, sTimer, bTimer;
        private int _autoCheckPeriod = 10; // sec
        public int autoCheckPeriod
        {
            get { return _autoCheckPeriod; }
            set { _autoCheckPeriod = value; dTimer.Interval = new TimeSpan(0, 0, _autoCheckPeriod); }
        }

        public bool Enabled = true;
        public int keyID { get; private set; }
        public bool Connected { get; private set; } // only for status purposes
        public bool partnerPresent
        {
            get
            {
                IntPtr hTargetWnd = NativeMethod.FindWindow(null, partner);
                return (hTargetWnd != IntPtr.Zero);
            }
        }

        public void Connect(string Partner, int _keyID = 666)
        {
            partner = Partner; keyID = _keyID; silentPartner = false;
            windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            HwndSource hwndSource = HwndSource.FromHwnd(windowHandle);
            hwndSource.AddHook(new HwndSourceHook(WndProc));
        }
        
        /// <summary>
        /// Establish communication channel 
        /// </summary>
        /// <param name="Partner">The title (caption) of the application</param>
        /// <param name="_keyID">Similar to port - one app can have more than one channel with diff. keyID</param>
        public RemoteMessaging()
        {
            Log = new memLog(); Log.Enabled = true; // for debug use 
            lastRcvMsg = ""; lastSndMsg = "";

            dTimer = new DispatcherTimer();
            dTimer.Tick += new EventHandler(dTimer_Tick);
            dTimer.Interval = new TimeSpan(0, 0, autoCheckPeriod);
            dTimer.Start();

            sTimer = new DispatcherTimer(DispatcherPriority.Send);
            sTimer.Tick += new EventHandler(sTimer_Tick);
            sTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);

            //stopClock();
        }

        #region Timing
        private void ResetTimer()
        {
            dTimer.Stop(); 
            if (Enabled) dTimer.Start();
        }
        /// <summary>
        /// Check regularly for connection status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dTimer_Tick(object sender, EventArgs e)
        {
            //if (!Enabled) return;
            CheckConnection(); //Console.WriteLine("Warning: the partner <"+partner+"> is not responsive!");
            //else Console.WriteLine("Info: the partner <" + partner + "> is responsive.");

            // Forcing the CommandManager to raise the RequerySuggested event
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

 /*
        public bool isClockRunning { get{return startTicks > 0;} }

        public void startClock(bool force = false) // if !force and the clock is running, go out
        {
            if (!force && isClockRunning) throw new Exception("The Clock is already running");
            startTicks = DateTime.Now.Ticks;
            if(Enabled) 
            {
                if (!sendCommand("SynchroClock:" + startTicks.ToString(),5)) throw new Exception("SynchroClock has not been accepted");
            }               
        }

        /// <summary>
        /// Precise timer for accurate time stamps
        /// </summary>
        /// <returns></returns>
        public double elapsedTime(bool required = false) // if required some real time must be back or exception; return in [s]; 
        {
            if (startTicks.Equals(-1))
            {
                if (required) throw new Exception("The Clock is NOT running");
                else return -1;
            }
            else return relTime(DateTime.Now.Ticks);
        }

        public double relTime(long ticks) // time relative to ticks
        {
            return (ticks - startTicks) / 10000000.0;
        }

        public void stopClock()
        {
            startTicks = -1;
        } */
        #endregion

        public delegate bool ReceiveHandler(string msg);
        public event ReceiveHandler OnReceive;
        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected bool Receive(string msg)
        {
            waiting4Reply = false;
            if (OnReceive != null) return OnReceive(msg);
            else return false;
        }

        public delegate void ActiveCommHandler(bool active, bool forced);
        public event ActiveCommHandler OnActiveComm;
        /// <summary>
        /// When the channel opens/closes
        /// </summary>
        /// <param name="active"></param>
        /// <param name="forced"></param>
        protected void ActiveComm(bool active, bool forced)
        {
            Connected = active;
            if (OnActiveComm != null) OnActiveComm(active, forced);
        }

        /// <summary>
        /// The wrapper around the actual Windows messaging receive part
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) // receive
        {
            if ((msg == WM_COPYDATA) && Enabled)
            {
                COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, (typeof(COPYDATASTRUCT)));
                if (cds.cbData == Marshal.SizeOf(typeof(MyStruct)))
                {
                    MyStruct myStruct = (MyStruct)Marshal.PtrToStructure(cds.lpData, typeof(MyStruct));
                    int msgID = myStruct.Number;
                    if (msgID == keyID)
                    {
                        lastRcvMsg = myStruct.Message;
                        Log.log("R: " + lastRcvMsg);
                        ResetTimer();
                        if (lastRcvMsg.IndexOf("SynchroClock") == 0) 
                        {
                            string[] da = lastRcvMsg.Split(':');
                            startTicks = Convert.ToInt64(da[1]);
                            return hwnd;
                        }
                        switch (lastRcvMsg) 
                        {
                            case("ping"):
                                handled = sendCommand("pong");
                                if (lastConnection != handled) OnActiveComm(handled, false); // fire only if the state has been changed
                                lastConnection = handled; Connected = handled;
                                break;
                            case("pong"):
                                handled = true;                               
                                break;
                        default:handled = Receive(lastRcvMsg); // the command systax is OK
                                break;
                        }
                    }
                    else handled = false;
                }
            }
            return hwnd;
        }

        private string json2send = "";
        private bool lastSentOK = true;
        /// <summary>
        /// Untangled by timer sending procedure
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sTimer_Tick(object sender, EventArgs e)
        {
            sTimer.Stop();
            if (json2send == "") throw new Exception("no message to be sent"); 
            lastSentOK = sendCommand(json2send);
            AsyncSent(lastSentOK,json2send);
        }

        public delegate void AsyncSentHandler(bool OK, string json2send);
        public event AsyncSentHandler OnAsyncSent;
        protected void AsyncSent(bool OK, string json2send)
        {
            if (OnAsyncSent != null) OnAsyncSent(OK, json2send);
        }
        public bool waiting4Reply = false;
        public MMexec sendAndReply(MMexec mme, bool async) // if async then wait inside, else follow the OnReceive event outside
        {
            waiting4Reply = true; int k = 0; lastRcvMsg = "";
            sendCommand(JsonConvert.SerializeObject(mme, Formatting.Indented));
            if (async)
            {
                while (waiting4Reply && (k < 50) && lastRcvMsg.Equals("")) // 5sec
                {
                    k++; Thread.Sleep(100); //Utils.DoEvents(); 
                }
            }
            if ((k > 45) || waiting4Reply || lastRcvMsg.Equals("")) return null; // timeout if async
            else return JsonConvert.DeserializeObject<MMexec>(lastRcvMsg);
        }

        /// <summary>
        /// The wrapper around the Windows messaging send part
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public bool sendCommand(string msg, int delay = 0) // [ms]
        {
            if (!Enabled) return false;
            if (delay > 0)
            {
                sTimer.Interval = new TimeSpan(delay*10000);
                json2send = msg;
                sTimer.Start();
                return true;
            }
            // Find the target window handle.
            IntPtr hTargetWnd = NativeMethod.FindWindow(null, partner);
            if (hTargetWnd == IntPtr.Zero)
            {
                Console.WriteLine("Unable to find the "+partner +" window");
                return false;
            }
            // Prepare the COPYDATASTRUCT struct with the data to be sent.
            MyStruct myStruct;

            myStruct.Number = keyID;
            myStruct.Message = msg; 

            // Marshal the managed struct to a native block of memory.
            int myStructSize = Marshal.SizeOf(myStruct);
            IntPtr pMyStruct = Marshal.AllocHGlobal(myStructSize);
            try
            {
                Marshal.StructureToPtr(myStruct, pMyStruct, true);

                COPYDATASTRUCT cds = new COPYDATASTRUCT();
                cds.cbData = myStructSize;
                cds.lpData = pMyStruct;

                // Send the COPYDATASTRUCT struct through the WM_COPYDATA message to 
                // the receiving window. (The application must use SendMessage, 
                // instead of PostMessage to send WM_COPYDATA because the receiving 
                // application must accept while it is guaranteed to be valid.)
                NativeMethod.SendMessage(hTargetWnd, WM_COPYDATA, windowHandle, ref cds);

                int result = Marshal.GetLastWin32Error();
                if (result != 0)
                {
                    Console.WriteLine(String.Format("SendMessage(WM_COPYDATA) failed w/err 0x{0:X}", result));
                    return false;
                }
                else
                {
                    lastSndMsg = msg; Log.log("S: " + lastSndMsg);
                    ResetTimer(); 
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("SendMessage(WM_COPYDATA) failed w/err: "+ e.Message);
                return false;
            } 
            finally
            {
                Marshal.FreeHGlobal(pMyStruct); 
            }
        }

        private bool lastConnection = false;
        /// <summary>
        /// ping<->pong to check the connection
        /// </summary>
        /// <param name="forced"></param>
        /// <returns></returns>
        public bool CheckConnection(bool forced = false)
        {
            if (!Enabled)
            {
                OnActiveComm(false, forced);
                Connected = false;
                return false;
            }
            lastRcvMsg = "";
            bool back = sendCommand("ping");
            if (back)
            {
                for (int i = 0; i < 200; i++)
                {                   
                    if (lastRcvMsg.Equals("pong")) break;
                    Thread.Sleep(10);
                }
            }
            back = back && (lastRcvMsg.Equals("pong"));
            if ((lastConnection != back) || forced) OnActiveComm(back, forced); // fire only if the state has been changed
            lastConnection = back; Connected = back;
            return back;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]       
        internal struct MyStruct
        {
            public int Number;

            [MarshalAs(UnmanagedType.ByValTStr , SizeConst = 1048576)]
            public string Message;
        }

        #region Native API Signatures and Types

        /// <summary>
        /// An application sends the WM_COPYDATA message to pass data to another 
        /// application.
        /// </summary>
        internal const int WM_COPYDATA = 0x004A;

        /// <summary>
        /// The COPYDATASTRUCT structure contains data to be passed to another 
        /// application by the WM_COPYDATA message. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct COPYDATASTRUCT
        {
            public IntPtr dwData;       // Specifies data to be passed
            public int cbData;          // Specifies the data size in bytes
            public IntPtr lpData;       // Pointer to data to be passed
        }

        internal class NativeMethod
        {
            /// <summary>
            /// Sends the specified message to a window or windows. The SendMessage 
            /// function calls the window procedure for the specified window and does 
            /// not return until the window procedure has processed the message. 
            /// </summary>
            /// <param name="hWnd">
            /// Handle to the window whose window procedure will receive the message.
            /// </param>
            /// <param name="Msg">Specifies the message to be sent.</param>
            /// <param name="wParam">
            /// Specifies additional message-specific information.
            /// </param>
            /// <param name="lParam">
            /// Specifies additional message-specific information.
            /// </param>
            /// <returns></returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SendMessage(IntPtr hWnd, int Msg,
                IntPtr wParam, ref COPYDATASTRUCT lParam);


            /// <summary>
            /// The FindWindow function retrieves a handle to the top-level window 
            /// whose class name and window name match the specified strings. This 
            /// function does not search child windows. This function does not 
            /// perform a case-sensitive search.
            /// </summary>
            /// <param name="lpClassName">Class name</param>
            /// <param name="lpWindowName">Window caption</param>
            /// <returns></returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        }
        #endregion
    }
    #region MMexec
    /// <summary>
    /// Class encapsulating one formated by "Book of JaSON" message
    /// </summary>
    public class MMexec
    {
        Random rnd = new Random();
        public string mmexec { get; set; }
        public enum SenderType { none, AxelHub, MotMaster, AxelProbe, AxelTilt }
        
        public SenderType getWhichSender()
        {           
            switch (sender)
            {
                case "Axel-hub": return SenderType.AxelHub;
                case "MotMaster": return SenderType.MotMaster;
                case "Axel-probe": return SenderType.AxelProbe;
                case "Axel-tilt": return SenderType.AxelTilt;
                default: return SenderType.none;
            }
        }
        public void setWhichSender(SenderType snd)  
        {
            switch (snd)
            {
                case SenderType.AxelHub:
                    sender = "Axel-hub";
                    break;
                case SenderType.MotMaster:
                    sender = "MotMaster";
                    break;
                case SenderType.AxelProbe:
                    sender = "Axel-probe";
                    break;
                case SenderType.AxelTilt:
                    sender = "Axel-tilt";
                    break;
                case SenderType.none:
                    sender = "";
                    break;
            }            
        }

        public string sender { get; set; }
        public string cmd { get; set; }
        public int id { get; set; }
        public Dictionary<string, object> prms;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="Caption"></param>
        /// <param name="Sender"></param>
        /// <param name="Command"></param>
        /// <param name="ID"></param>
        public MMexec(string Caption = "", string Sender = "", string Command = "", int ID = -1)
        {
            mmexec = Caption;
            sender = Sender;
            cmd = Command;
            if (ID == -1) id = rnd.Next(int.MaxValue);
            else id = ID;
            prms = new Dictionary<string, object>();
        }
        /// <summary>
        /// Clean all up
        /// </summary>
        public void Clear()
        {
            mmexec = "";
            sender = "";
            cmd = "";
            id = -1;
            prms.Clear();
        }
        /// <summary>
        /// Assign src to this
        /// </summary>
        /// <param name="src"></param>
        public void Assign(MMexec src)
        {
            mmexec = src.mmexec;
            sender = src.sender;
            cmd = src.cmd;
            id = src.id;
            prms = new Dictionary<string, object>(src.prms);
        }
        /// <summary>
        /// Clone this - copy all the props to a new instance
        /// </summary>
        /// <returns></returns>
        public MMexec Clone()
        {
            MMexec mm = new MMexec();
            mm.mmexec = mmexec;
            mm.sender = sender;
            mm.cmd = cmd;
            mm.id = id;
            mm.prms = new Dictionary<string, object>(prms);
            return mm;
        }

        /// <summary>
        /// Standard Abort (Cancel) message
        /// </summary>
        /// <param name="Sender"></param>
        /// <returns></returns>
        public string Abort(string Sender = "")
        {
            cmd = "abort";
            mmexec = "abort!Abort!ABORT!";
            if (!Sender.Equals("")) sender = Sender;
            prms.Clear();
            return JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// Properties part of MMscan
    /// </summary>
    public class baseMMscan
    {
        public baseMMscan()
        {

        }
        public baseMMscan(string _sParam = "", double _sFrom = double.NaN, double _sTo = double.NaN, double _sBy = double.NaN, double _Value = double.NaN)
        {
            sParam = _sParam; sFrom = _sFrom; sTo = _sTo; sBy = _sBy; Value = _Value;
        }
        public baseMMscan(string inStr)
        {
            this.setAsString(inStr);
        }        
        public string sParam { get; set; }
        public double sFrom { get; set; }
        public double sTo { get; set; }
        public double sBy { get; set; }
        public double Value { get; set; }
        public string comment { get; set; }

        public string getAsString()
        {
            string ss = sParam + "  \t" + sFrom.ToString("G6") + " .. " + sTo.ToString("G6") + "; " + sBy.ToString("G6");
            if (!Utils.isNull(comment))
                if (!comment.Equals("")) ss += " #" + comment;
            return ss;
        }
        public void setAsString(string inStr)
        { 
            // optional
            string[] parts = inStr.Split('#'); if (parts.Length == 2) comment = parts[1].Trim(' ');
            parts = parts[0].Split('='); if (parts.Length == 2) Value = Convert.ToDouble(parts[1].Trim(' '));
            // must
            parts = parts[0].Split('\t'); sParam = parts[0].TrimEnd(' ');
            string ss = parts[1]; int j = ss.IndexOf(".."); if (j == -1) return;
            parts[0] = ss.Substring(0, j); parts[1] = ss.Substring(j + 2);
            sFrom = Convert.ToDouble(parts[0]);
            parts = parts[1].Split(';'); if (parts.Length != 2) return;
            sTo = Convert.ToDouble(parts[0]);
            sBy = Convert.ToDouble(parts[1]);
        }
    }
    
    /// <summary>
    /// Class encapsulating a scan of a parameter and some context
    /// </summary>
    public class MMscan : baseMMscan
    {
        public string groupID { get; set; }
        public string sSite { get; set; }
        public bool randomized;

        public MMscan(string _groupID = "", string _sSite = "", string _sParam = "", double _sFrom = double.NaN, double _sTo = double.NaN, double _sBy = double.NaN, double _Value = double.NaN)
        {
            groupID = _groupID; sSite = _sSite; sParam = _sParam; sFrom = _sFrom; sTo = _sTo; sBy = _sBy; Value = _Value; randomized = false;
        }
        public bool Check()
        {
            if ((sFrom == sTo) || (sBy == 0) || (Math.Abs(sBy) > Math.Abs(sTo - sFrom))) return false;
            if ((sBy > 0) && (sFrom > sTo)) return false;
            if ((sBy < 0) && (sFrom < sTo)) return false;
            return true;
        }

        public bool isFirstValue()
        {
            return Math.Abs(sFrom - Value) < (0.01 * sBy);
        }

        public bool isLastValue()
        {
            return (Math.Abs(sTo - Value)) < (0.99 * sBy);
        }

        public MMscan NextInChain = null;
        /// <summary>
        /// The main call when scan, 
        /// it works multiscan (chain by NextInChain of scans) mode
        /// The latter uses recursive (on class level) call the same Next down the chain 
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            bool NextValue = false;
            if (NextInChain != null)
            {
                NextValue = NextInChain.Next();
            }
            if (NextValue) return true;
            else
            {
                Value += sBy;
                if (Value > sTo)
                {
                    Value = sFrom;
                    return false;
                }
                else return true;
            }
        }

        /// <summary>
        /// Package / unpackage the scan params as string (multi-scan mostly)
        /// </summary>
        public string AsString
        {
            get { return getAsString(); }
            set
            {
                if (value is null) return;
                if (value == "")
                {
                    TestInit(); return;
                }
                setAsString(value);
            }
        }
        /// <summary>
        /// Assign src to this
        /// </summary>
        /// <param name="src"></param>
        public void Assign(MMscan src)
        {
            groupID = src.groupID; sSite = src.sSite; sParam = src.sParam; sFrom = src.sFrom; sTo = src.sTo; sBy = src.sBy; Value = src.Value; comment = src.comment;
            randomized = src.randomized;
        }
        /// <summary>
        /// Clone this - copy all the props to a new instance
        /// </summary>
        /// <returns></returns>
        public MMscan Clone()
        {
            return new MMscan(groupID, sSite, sParam, sFrom, sTo, sBy, Value);
        }

        /// <summary>
        /// Fill in something - only for tests
        /// </summary>
        public void TestInit()
        {
            groupID = DateTime.Now.ToString("yy-MM-dd_H-mm-ss");
            sSite = "";
            sParam = "prm";
            sFrom = 0;
            sTo = 4 * 3.14;
            sBy = 0.1;
        }
        /// <summary>
        /// Fill in dictionary with props
        /// </summary>
        /// <param name="dict"></param>
        public void ToDictionary(ref Dictionary<string, object> dict)
        {
            if (dict is null) dict = new Dictionary<string, object>();
            dict["groupID"] = groupID;
            dict["site"] = sSite;
            dict["param"] = sParam;
            dict["from"] = sFrom;
            dict["to"] = sTo;
            dict["by"] = sBy;
            if (randomized) dict["random"] = true;
        }
        /// <summary>
        /// Update props from a dictionary
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool FromDictionary(Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("groupID")) groupID = Convert.ToString(dict["groupID"]);
            else return false;
            if (dict.ContainsKey("site")) sSite = Convert.ToString(dict["site"]); // optional
            else sSite = "";
            if (dict.ContainsKey("param")) sParam = Convert.ToString(dict["param"]);
            else return false;
            if (dict.ContainsKey("random")) randomized = Convert.ToBoolean(dict["random"]); // optional
            else randomized = false;
            if (dict.ContainsKey("from")) sFrom = Convert.ToDouble(dict["from"]);
            else return false;
            if (dict.ContainsKey("to")) sTo = Convert.ToDouble(dict["to"]);
            else return false;
            if (dict.ContainsKey("by")) sBy = Convert.ToDouble(dict["by"]);
            else return false;
            return true;
        }
    }
    #endregion MMexec

    #region The Time
    /// <summary>
    /// Time from system.ticks routines
    /// </summary>
    public static class theTime
    {
        public static long startSeqSeries = -1;

        public static void startTime(bool forced = false)
        {
            if (isTimeRunning)
            {
                if (forced) startSeqSeries = DateTime.Now.Ticks; // 1 tick is 100[ns]
            }
            else startSeqSeries = DateTime.Now.Ticks;
        }
        public static void stopTime()
        {
            startSeqSeries = -1;
        }
        public static bool isTimeRunning
        {
            get { return startSeqSeries > 0; }
        }
        public static double elapsedTime // [sec]
        {
            get
            {
                if (!isTimeRunning) throw new Exception("The main clock is not running.");
                return Utils.tick2sec(DateTime.Now.Ticks - startSeqSeries);
            }
        }
        public static double relativeTime(long relative) // reference to "relative [tick]" [sec]
        {
            if (!isTimeRunning) throw new Exception("The main clock is not running.");
            return Utils.tick2sec(relative - startSeqSeries);
        }
    }
    #endregion The Time

    #region ImgUtl
    public static class ImgUtl
    {
        public static void VisualCompToPng(UIElement element, string filename = "")
        {
            var rect = new Rect(element.RenderSize);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(element), null, rect);
            }
            var bitmap = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);

            if (filename.Equals(""))
            {
                Clipboard.SetImage(bitmap);
                Utils.TimedMessageBox("The image is in the clipboard");
            }
            else
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var file = File.OpenWrite(filename))
                {
                    encoder.Save(file);
                }
            }
        }
        /*public static void copyGraphToClipboard(NationalInstruments.Controls.Graph gr, string filename)
        {
            Rect bounds; RenderTargetBitmap bitmap;
            bounds = System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot(gr);
            bitmap = new RenderTargetBitmap((int)bounds.Width + 50, (int)bounds.Height + 50, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(gr);
            if (filename.Equals(""))
            {
                Clipboard.SetImage(bitmap);
                Utils.TimedMessageBox("The image is in the clipboard");
            }
            else
            {
                var encoder = new PngBitmapEncoder();
                BitmapFrame frame = BitmapFrame.Create(bitmap);
                encoder.Frames.Add(frame);
                using (var stream = File.Create(filename))
                {
                    encoder.Save(stream);
                }
            }
        }*/
    }
    #endregion ImgUtl
}
