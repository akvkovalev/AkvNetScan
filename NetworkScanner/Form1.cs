using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using AkvNetScan.classes;
using System.IO;
using System.Threading;

using System.Diagnostics;
using System.Net.Sockets;

namespace NetworkScanner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // Инициализация формы

            Control.CheckForIllegalCrossThreadCalls = false;
            
            tblexp.Click += Tblexp_Click;

        }

        private int count = 0;

        // Асинхронная функция проверки IP-адресов
        public async Task ScanAsync(string start, string end)
        {
            try
            {
                string[] startIPString = start.Split('.');
                int[] startIP = Array.ConvertAll(startIPString, int.Parse);
                string[] endIPString = end.Split('.');
                int[] endIP = Array.ConvertAll(endIPString, int.Parse);

                int startIp = startIP[2] * 256 + startIP[3];
                int endIp = endIP[2] * 256 + endIP[3];
                int totalAddresses = (endIp - startIp) + 1;

                progressBar1.Maximum = totalAddresses;
                progressBar1.Value = 0;
                listVAddr.Items.Clear();
                listVAddr.ListViewItemSorter = new IpAddressComparer();

                var semaphore = new SemaphoreSlim(200); // Ограничение на 200 параллельных пингов
                var uiBatch = new List<ListViewItem>(); // Для накопления данных перед добавлением в UI

                async Task ProcessAddress(string ipAddress)
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await PingAndResolveAsync(ipAddress, uiBatch);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                var tasks = new List<Task>();

                for (int i = startIP[2]; i <= endIP[2]; i++)
                {
                    for (int y = startIP[3]; y <= 255; y++)
                    {
                        string ipAddress = startIP[0] + "." + startIP[1] + "." + i + "." + y;
                        string endIPAddress = endIP[0] + "." + endIP[1] + "." + endIP[2] + "." + (endIP[3] + 1);

                        if (ipAddress == endIPAddress)
                        {
                            break;
                        }

                        tasks.Add(ProcessAddress(ipAddress));

                        if (tasks.Count >= 2000) // Пакетный запуск для уменьшения потребления памяти
                        {
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                            UpdateUI(uiBatch); // Обновляем UI только для накопленных данных
                        }
                    }
                    startIP[3] = 1;
                }

                await Task.WhenAll(tasks);
                UpdateUI(uiBatch);

                MessageBox.Show($"Сканирование завершено! Найдено {count} устройств.", "Инфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неверный диапазон", "Инфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateUI(List<ListViewItem> batch)
        {
            if (batch.Count > 0)
            {
                listVAddr.Items.AddRange(batch.ToArray());
                progressBar1.Value += batch.Count;
                batch.Clear();
            }
        }

        public class MyModel
        {
            // Ваши члены данных модели, например:
            public string Property1 { get; set; }
            public int Property2 { get; set; }

            // Конструктор, если необходимо
            public MyModel(string property1, int property2)
            {
                Property1 = property1;
                Property2 = property2;
            }
        }

        private async Task PingAndResolveAsync(string ipAddress, List<ListViewItem> uiBatch)
        {
            Ping myPing = new Ping();

            try
            {
                PingReply reply = await myPing.SendPingAsync(ipAddress, 250);

                if (reply.Status == IPStatus.Success)
                {
                    try
                    {
                        IPAddress addr = IPAddress.Parse(ipAddress);
                        IPHostEntry host = await Dns.GetHostEntryAsync(addr);

                        string hostName = host.HostName;

                        uiBatch.Add(new ListViewItem
                        {
                            Text = ipAddress,
                            SubItems = { hostName, "Up" }
                        });

                        count++;
                    }
                    catch
                    {
                        uiBatch.Add(new ListViewItem
                        {
                            Text = ipAddress,
                            SubItems = { "Unknown", "Up" }
                        });
                    }
                }
                else
                {
                    uiBatch.Add(new ListViewItem
                    {
                        Text = ipAddress,
                        SubItems = { "n/a", "Down" }
                    });
                }
            }
            catch
            {
                
            }
        }


        // Обработчик кнопки "Сканировать"
        private async void CmdScan_Click(object sender, EventArgs e)
        {
            if (txtIP.Text == string.Empty)
            {
                MessageBox.Show("IP не введен", "Инфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                cmdScan.Enabled = false;

                txtIP.Enabled = false;


                try
                {
                    await ScanAsync(txtIP.Text, txtIP2.Text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    // Блок finally выполняется всегда, независимо от того, как завершено сканирование
                    cmdScan.Enabled = true;

                    txtIP.Enabled = true;
                }
            }
        }



       


       

        private void Tblexp_Click(object sender, EventArgs e)
        {
            ExportToCsv();
        }

        private void ExportToCsv()
        {
            try
            {
                // SaveFileDialog CSV file
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV файлы (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog.Title = "Экспорт в CSV";
                saveFileDialog.ShowDialog();

                
                if (saveFileDialog.FileName != "")
                {
                    // Create a StreamWriter to write to the CSV file
                    using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                    {
                        // Write the header line
                        sw.WriteLine("IP Address,Host Name,Status");

                        // Write each line of data
                        foreach (ListViewItem item in listVAddr.Items)
                        {
                            sw.WriteLine($"{item.SubItems[0].Text},{item.SubItems[1].Text},{item.SubItems[2].Text}");
                        }
                    }

                    MessageBox.Show("Экспорт в CSV выполнен успешно.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта CSV: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}