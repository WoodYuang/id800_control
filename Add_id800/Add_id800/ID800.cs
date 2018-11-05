using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Add_id800
{
    class ID800
    {
        [DllImport("tdcbase.dll")]
        private static extern double TDC_getVersion();
        [DllImport("tdcbase.dll")]
        private static extern double TDC_getTimebase();
        [DllImport("tdcbase.dll")]
        private static extern int TDC_switchTermination(int on);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_init(int deciceID);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_deInit();
        [DllImport("tdcbase.dll")]
        private static extern int TDC_enableChannels(int channelMask);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_setTimestampBufferSize(int size);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_getLastTimestamps(int reset, ref long timestamps, ref byte channels, ref int valid);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_getCoincCounters(ref int data);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_getHistogram(int chanA, int chanB, int reset, ref int data, ref int count, ref int tooSmall, ref int tooLarge, ref int eventsA, ref int eventsB, ref long expTime);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_setHistogramParams(int binWidth, int binCount);
        [DllImport("tdcbase.dll")]
        private static extern int TDC_clearAllHistograms();

        public double myTimebase { get; set; }
        public int myTermination { get; set; }               //1为50欧姆内阻；0为高阻
        public int myChannels { get; set; }
        public bool working { get; set; }
        public const int BufferSize = 1000000;
        public TimeSpan myIntegralTime { get; set; }
        private delegate void UIChange();


        //ID800初始化
        public ID800()
        {
            TDC_deInit();
            myTermination = 1;    //默认为50欧姆内阻
            myChannels = 3 ;      //默认为打开2个通道 11
            working = false;
            myTimebase = TDC_getTimebase();       //@brief Get Time Base
            TDC_init(-1); //@brief Initialize and Start
        }   //构造函数

        public int close()
        {
            return (TDC_deInit());//@brief Disconnect and uninitialize
        }

        public void start()
        {
            TDC_switchTermination(myTermination);    //@brief Switch Input Termination
            TDC_setTimestampBufferSize(BufferSize);  //@brief Set Timestamp Buffersize
            TDC_enableChannels(myChannels);    ///@brief Enable TDC Channels
        }

        public void clear()
        {
            long[] tem1 = new long[BufferSize];
            byte[] tem2 = new byte[BufferSize];
            int valid = 0;
            TDC_getLastTimestamps(1, ref tem1[0], ref tem2[0], ref valid);//@brief Retreive Last Timestamp Values
        }

        public void trytry(ref int channel1, ref int channel2, ref int coin0, double delay0,double coinWin, int IntegralTime,ref int valid0)
        {
            coin0 = 0;
            valid0 = 0;
            long[] stamp = new long[BufferSize + 1];
            byte[] channels = new byte[BufferSize + 1];
            long[] stamp0 = new long[BufferSize + 1];
            byte[] channels0 = new byte[BufferSize + 1];

            requiredata(out stamp, out channels, out valid0, delay0, IntegralTime);
            channel1 = 0;
            channel2 = 0;

            for (int i = 0; i < valid0; i++)
            {
                if (channels[i] == 0)
                    channel1++;
                if (channels[i] == 1)
                    channel2++;
            }

            stamp.CopyTo(stamp0, 0);
            channels.CopyTo(channels0, 0);

            long coinWinbin = (long)Math.Round(coinWin * 0.000000001 / myTimebase);    //符合窗口以纳秒为单位

            int coin = 0;
            for (int i = 0; i < valid0; i++)
            {
                if (channels0[i] == 1)
                {
                    for (int j = i + 1; j < valid0; j++)
                    {
                        if (channels0[j] == 2)
                        {
                            if (stamp0[j] - stamp0[i] <= coinWinbin)
                            {
                                coin++;
                                channels0[j] = 255;
                                break;
                            }
                            else
                                break;
                        }
                    }
                }
            }
            coin0 = coin;
        }

        //数据获取
        public void requiredata(out long[] stamp, out byte[] channels, out int valid, double delay0, int IntegralTime)
        {
            working = true;

            //double periodTime = 500; //读两个周期, 500ms

            stamp = new long[BufferSize + 1];
            channels = new byte[BufferSize + 1];
            valid = 0;
            long[] tem1 = new long[BufferSize];
            byte[] tem2 = new byte[BufferSize];
            long[] stamp1 = new long[BufferSize + 1];
            byte[] channels1 = new byte[BufferSize + 1];

            //读两个周期的数据
            long periodticks = Convert.ToInt64(IntegralTime * TimeSpan.TicksPerMillisecond);
            TDC_getLastTimestamps(1, ref tem1[0], ref tem2[0], ref valid);      //清除数据
            DateTime StartTime = DateTime.Now;
            do
            {
                TimeSpan runLength = DateTime.Now.Subtract(StartTime);
                long runticks = runLength.Ticks;

                if (runticks > periodticks)
                {
                    break;
                }
                Application.DoEvents();
            }
            while (true);

            TDC_getLastTimestamps(1, ref stamp[0], ref channels[0], ref valid);
            delaysort(stamp, channels, delay0, valid);
        }
        //数据获取结束

        public void delaysort(long[] st, byte[] ch, double delay0, long valid)
        // CH1：Trigger信号； CH2：闲置光信号； CH3：透射光信号； CH4：反射光信号
        // delay1是CH3相对CH2的延迟
        // delay2是CH4相对CH2的延迟
        // delay0是CH1相对CH2的延迟
        {
            long pcs0 = (long)Math.Round(delay0 * 0.000001 / myTimebase);
            long pcs00 = (long)Math.Round(1.5 * 0.000000001 / myTimebase);   //前后3ns
            pcs0 = pcs0 + pcs00;
            int i, j;
            for (i = 0; i < valid; i++)
            {
                if (ch[i] == 0)             //CH1相对CH2的延迟
                    st[i] += pcs0;
            }

            //冒泡排序
            long sttem;
            byte chtem;
            for (i = 0; i < valid; i++)
            {
                for (j = 0; j < valid - i; j++)
                {
                    if (st[j] > st[j + 1])
                    {
                        sttem = st[j + 1];
                        st[j + 1] = st[j];
                        st[j] = sttem;
                        chtem = ch[j + 1];
                        ch[j + 1] = ch[j];
                        ch[j] = chtem;
                    }
                }
            }

            //sort(st, ch, 0, valid - 1);  //排序
        }
    }
}
