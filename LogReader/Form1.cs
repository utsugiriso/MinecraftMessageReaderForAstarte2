using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using FNF.Utility;
using System.Diagnostics;


namespace LogReader
{
    public partial class Form1 : Form
    {
        private const string chatQuery = "[Client thread/INFO]: [CHAT] ";
        private static int LineCount = 0;
        private static BouyomiChanClient bc = new BouyomiChanClient();

        private delegate void Read(List<String> lineList);

        private string getFilePath()
        {
            return Path.Combine(this.fileSystemWatcher1.Path, this.fileSystemWatcher1.Filter);
        }

        public Form1()
        {
            InitializeComponent();
            this.fileSystemWatcher1.Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft/logs");

            using (FileStream fileStream = new FileStream(getFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fileStream, System.Text.Encoding.Default))
                {
                    while (reader.Peek() > -1)
                    {
                        reader.ReadLine();
                        LineCount++;
                    }
                }
            }

            this.comboBox1.DataSource = Enum.GetValues(typeof(VoiceType));
            this.comboBox2.DataSource = Enum.GetValues(typeof(VoiceType));

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.ReadOnly = false;
            dataGridView1.ColumnCount = 3;
            dataGridView1.Columns[0].Name = "CharacterName";
            dataGridView1.Columns[0].DataPropertyName = "CharacterName";
            dataGridView1.Columns[1].Name = "Tone";
            dataGridView1.Columns[1].DataPropertyName = "Tone";
            dataGridView1.Columns[2].Name = "Volume";
            dataGridView1.Columns[2].DataPropertyName = "Volume";
            DataGridViewComboBoxColumn voiceTypeColumn = new DataGridViewComboBoxColumn();
            dataGridView1.Columns.Insert(1, voiceTypeColumn);
            voiceTypeColumn.Name = "VoiceType";
            voiceTypeColumn.DataPropertyName = "VoiceType";
            voiceTypeColumn.DataSource = Enum.GetValues(typeof(VoiceType));
            //voiceTypeColumn.ValueMember = ;
            //voiceTypeColumn.DisplayMember = ;
            //voiceTypeColumn.HeaderText = columnName;
            //voiceTypeColumn.DropDownWidth = 160;
            //voiceTypeColumn.Width = 90;
            //voiceTypeColumn.MaxDropDownItems = Enum.GetNames(typeof(VoiceType)).Length;
            //voiceTypeColumn.FlatStyle = FlatStyle.Flat;

            List<ReaderData> readerDataList = new List<ReaderData>();
            if(Properties.Settings.Default.ReaderData != null)
            {
                foreach (string readerDataString in Properties.Settings.Default.ReaderData)
                {
                    string[] splited = readerDataString.Split(',');
                    string characterName = splited[0];
                    string voiceType = splited[1];
                    string tone = splited[2];
                    string volume = splited[3];
                    readerDataList.Add(new ReaderData(characterName, (VoiceType)Enum.Parse(typeof(VoiceType), voiceType), int.Parse(tone), int.Parse(volume)));
                }
            }
            if (!String.IsNullOrEmpty(Properties.Settings.Default.DefaultReaderData))
            {
                string[] splited = Properties.Settings.Default.DefaultReaderData.Split(',');
                comboBox2.SelectedIndex = int.Parse(splited[0]);
                numericUpDown2.Value = decimal.Parse(splited[1]);
                numericUpDown3.Value = decimal.Parse(splited[2]);
            }

            BindingSource bs = new BindingSource();
            bs.DataSource = readerDataList;
            dataGridView1.DataSource = bs;

            Process process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = String.Format("Get-Content -Path \"{0}\" -Tail 0 -Wait", getFilePath());
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                try
                {
                    if (e.Data.Contains(chatQuery))
                    {
                        List<String> lineList = new List<string>();
                        lineList.Add(e.Data);
                        Invoke(new Read(read), lineList);
                    }
                }
                catch
                {
                    Application.Exit();
                }
            });
            process.Start();
            process.BeginOutputReadLine();

            Application.ApplicationExit += new EventHandler((sender, e) =>
            {
                saveSettings();
                if (!process.HasExited)
                    process.CloseMainWindow();
                bc.Dispose();
            });
        }

        private void read(List<String> lineList)
        {
            foreach (string line in lineList)
            {
                int chatStartIndex = line.IndexOf(chatQuery) + chatQuery.Length;
                string characterName = null;
                string message = null;
                ReaderData readerData = null;

                int messageStartIndex = line.IndexOf("「");
                if (messageStartIndex == -1)
                {
                    messageStartIndex = line.IndexOf(">");
                    if (messageStartIndex != -1)
                        messageStartIndex++;
                }
                if (messageStartIndex == -1)
                {
                    message = line.Substring(chatStartIndex);
                }
                else
                {
                    int subStringCount = messageStartIndex - chatStartIndex;
                    characterName = line.Substring(chatStartIndex, subStringCount).Trim();
                    if (characterName[0].Equals('<'))
                        characterName = characterName.Substring(1);
                    if (characterName[characterName.Length - 1].Equals('>'))
                        characterName = characterName.Substring(0, characterName.Length - 1);
                    message = line.Substring(messageStartIndex);
                }

                if (String.IsNullOrEmpty(characterName))
                {
                    readerData = new ReaderData("", (VoiceType)Enum.ToObject(typeof(VoiceType), comboBox2.SelectedValue), decimal.ToInt32(numericUpDown2.Value), decimal.ToInt32(numericUpDown3.Value));
                }
                else
                {
                    foreach (ReaderData _readerData in (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource)
                    {
                        if (_readerData.CharacterName.Equals(characterName))
                        {
                            readerData = _readerData;
                            break;
                        }
                    }
                }

                if (readerData == null && !String.IsNullOrEmpty(characterName))
                {
                    ((BindingSource)dataGridView1.DataSource).Add(new ReaderData(characterName, (int)VoiceType.Default, ReaderData.TONE_DEFAULT));
                    saveSettings();
                }

                addTalkTask(message, readerData);
            }
        }

        private void addTalkTask(string message, ReaderData readerData)
        {
            if(readerData == null)
                bc.AddTalkTask(message, ReaderData.SPEED_DEFAULT, ReaderData.TONE_DEFAULT, decimal.ToInt32(numericUpDown5.Value), VoiceType.Default);
            else
                bc.AddTalkTask(message, readerData.GetAdjustedSpeed(), readerData.Tone, readerData.Volume * decimal.ToInt32(numericUpDown5.Value) / 100, readerData.VoiceType);
        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (dataGridView1.DataSource == null)
                return;

            return; // comment outed

            int lineCount = 0;
            List<String> lineList = new List<string>();

            using (FileStream fileStream = new FileStream(getFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fileStream, System.Text.Encoding.Default))
                {
                    while (reader.Peek() > -1)
                    {
                        string line = reader.ReadLine();
                        if (LineCount < lineCount)
                        {
                            if (line.Contains(chatQuery))
                            {
                                lineList.Add(line);
                            }
                        }
                        lineCount++;
                    }
                }
            }
            LineCount = lineCount;

            if (lineList.Count != 0)
                read(lineList);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox1.Text))
                return;

            bool exists = false;
            foreach(ReaderData readerData in (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource)
            {
                if(readerData.CharacterName.Equals(textBox1.Text))
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                ((BindingSource)dataGridView1.DataSource).Add(new ReaderData(textBox1.Text, (VoiceType)Enum.ToObject(typeof(VoiceType), comboBox1.SelectedValue), decimal.ToInt32(numericUpDown1.Value), decimal.ToInt32(numericUpDown4.Value)));
                saveSettings();
            }
        }

        private List<String> getSelectedCharacterNameList()
        {
            List<ReaderData> readerDataList = (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource;
            List<string> characterNameList = new List<string>();
            foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
            {
                if (!characterNameList.Contains(readerDataList[cell.RowIndex].CharacterName))
                {
                    characterNameList.Add(readerDataList[cell.RowIndex].CharacterName);
                }
            }

            return characterNameList;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<string> deleteCharacterNameList = getSelectedCharacterNameList();

            foreach(string deleteCharacterName in deleteCharacterNameList)
            {
                foreach(ReaderData readerData in (BindingSource)dataGridView1.DataSource)
                {
                    if (deleteCharacterName.Equals(readerData.CharacterName))
                    {
                        ((BindingSource)dataGridView1.DataSource).Remove(readerData);
                        break;
                    }
                }
            }

            saveSettings();
        }

        private void saveSettings()
        {
            StringCollection stringCollection = new StringCollection();
            foreach (ReaderData readerData in (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource)
            {
                stringCollection.Add(readerData.CharacterName + "," + readerData.VoiceType.ToString() + "," + readerData.Tone.ToString() + "," + readerData.Volume.ToString());
            }
            Properties.Settings.Default.ReaderData = stringCollection;
            Properties.Settings.Default.DefaultReaderData = comboBox2.SelectedIndex + "," + numericUpDown2.Value + "," + numericUpDown3.Value;
            Properties.Settings.Default.Save();
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            saveSettings();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<string> deleteCharacterNameList = getSelectedCharacterNameList();
            foreach (string deleteCharacterName in deleteCharacterNameList)
            {
                foreach (ReaderData readerData in (BindingSource)dataGridView1.DataSource)
                {
                    if (deleteCharacterName.Equals(readerData.CharacterName))
                    {
                        addTalkTask("テスト", readerData);
                        break;
                    }
                }
            }
        }
    }
}
