using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Add_id800
{
    public partial class Form1 : Form
    {
        //ID800声明
        ID800 id800;
        private delegate void UIChange();  //委托类型
        double delay, coinWin;
        int valid0;
        private bool try0 = true;


        private void Form1_Load(object sender, EventArgs e)
        {
            //在窗口上显示
            textBox4.Text = "1000";
            textBox5.Text = "0";
            textBox6.Text = "3";

            id800 = new ID800();
            ID800_ini();

            //参数读入
            delay = Convert.ToDouble(textBox5.Text.Trim());
            coinWin = Convert.ToDouble(textBox6.Text.Trim());



            //启动ID800
            id800.start();
            button1.Enabled = true;


        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        public Form1()
        {
            InitializeComponent();
        }

        private void ID800_ini()
        {
            int channel = 1 + 2;        //确定只用2个通道，二进制下为 11
            id800.myChannels = channel;
            id800.myTermination = 1;            //确定为50欧姆
            id800.myIntegralTime = new TimeSpan((long)Math.Round(double.Parse(textBox4.Text) * 10000));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try0 = false;
            for (int i = 0; i < 100000; i++)
            {
                System.Windows.Forms.Application.DoEvents();
            }
            id800.close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try0 = true;

            int IntegralTime = int.Parse(textBox4.Text);
            id800.start();
            long Integralticks = Convert.ToInt64(IntegralTime * TimeSpan.TicksPerMillisecond);

            do
            {
                //符合
                int channel1 = 0;
                int channel2 = 0;
                int coin = 0;


                id800.trytry(ref channel1, ref channel2, ref coin, delay, coinWin, IntegralTime,ref valid0);
                textBox1.Text = channel1.ToString();
                textBox2.Text = channel2.ToString();
                textBox3.Text = coin.ToString();
                textBox7.Text = valid0.ToString();

                DateTime StartTime = DateTime.Now;
                do
                {
                    TimeSpan runLength = DateTime.Now.Subtract(StartTime);
                    long runticks = runLength.Ticks;

                    if (runticks > Integralticks)
                    {
                        break;
                    }
                    System.Windows.Forms.Application.DoEvents();
                }
                while (true);
            } while (try0 == true);
        }



    }
}
