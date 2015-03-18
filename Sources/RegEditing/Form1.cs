using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RegEditing
{
    public partial class Form1 : Form
    {
        #region 属性和委托

        string[] strSelectArray = new string[] { "选择操作", "修改3389端口", "开启或关闭默认共享" };

        function[] funs;
        functionDoing[] funs_Doing;

        delegate void function();
        delegate string functionDoing();

        #endregion

        #region 构造
        public Form1()
        {
            InitializeComponent();
        }

        #endregion

        #region 事件
        private void Form1_Load(object sender, EventArgs e)
        {
            init();
            cmbSelect.SelectedIndex = 0;
        }

        private void btnDoIt_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show(funs_Doing[cmbSelect.SelectedIndex]());
                funs[cmbSelect.SelectedIndex]();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                funs[cmbSelect.SelectedIndex]();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region 方法

        #region 初始化
        private void init()
        {
            lblStatus.Text = "";
            cmbSelect.Items.Clear();
            cmbSelect.Items.AddRange(strSelectArray);

            funs = new function[]{
                   new function(delegate{
                   lblStatus.Text = "请选择需要的操作";
               }),
                   new function(delegate{
                       lblStatus.Text = String.Format("当前操作:修改远程管理端口,默认为3389\r\n当前值为: {0} 和 {1}", 
                           GetValue(RegistryHive.LocalMachine, 
                                    @"SYSTEM\CurrentControlSet\Control\Terminal Server\Wds\rdpwd\Tds\tcp", 
                                    "PortNumber", 
                                    RegistryValueKind.DWord),
                           GetValue(RegistryHive.LocalMachine, 
                                    @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", 
                                    "PortNumber", 
                                    RegistryValueKind.DWord));
               }),
                   new function(delegate{
                       lblStatus.Text = String.Format("当前操作:开启或关闭远程访问默认共享]\r\n0或空：关闭默认共享，1：开启默认共享\r\n当前值为: {0}",
                           GetValue(RegistryHive.LocalMachine, 
                                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", 
                                    "LocalAccountTokenFilterPolicy", 
                                    RegistryValueKind.DWord));
               })
            };

            funs_Doing = new functionDoing[] { 
                nothing,
                edit3389,
                defaultShare
            };
        }
        #endregion

        #region 目的方法
        private string nothing()
        {
            return "请选择需要的操作";
        }

        private string edit3389()
        {
            string ex = "";
            try
            {
                string temp = tbValue.Text;
                int intValue = 0;
                if (!int.TryParse(temp, out intValue) || intValue < 1024 || intValue > 65535)
                {
                    return "输入的值不正确,新端口号应为大于1024且小于65535的整数";
                }

                SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server\Wds\rdpwd\Tds\tcp", "PortNumber", intValue.ToString(), RegistryValueKind.DWord, out ex);
                SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "PortNumber", intValue.ToString(), RegistryValueKind.DWord, out ex);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return "操作成功";
        }

        private string defaultShare()
        {
            string ex = "";
            try
            {
                string temp = tbValue.Text;
                int intValue = 0;
                if (!int.TryParse(temp, out intValue) || (intValue != 0 && intValue != 1))
                {
                    return "输入的值不正确,只接受0或1";
                }

                SetValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "LocalAccountTokenFilterPolicy", intValue.ToString(), RegistryValueKind.DWord, out ex);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return "操作成功";
        }
        #endregion

        #region 读取和写入注册表
        private bool SetValue(RegistryHive regk, string path, string theKey, string theValue, RegistryValueKind ValueKind, out string ex)
        {
            try
            {
                RegistryKey root = null;
                switch (regk)
                {
                    case RegistryHive.ClassesRoot:
                        root = Registry.ClassesRoot;
                        break;
                    case RegistryHive.CurrentConfig:
                        root = Registry.CurrentConfig;
                        break;
                    case RegistryHive.CurrentUser:
                        root = Registry.CurrentUser;
                        break;
                    case RegistryHive.DynData:
                        root = Registry.DynData;
                        break;
                    case RegistryHive.LocalMachine:
                        root = Registry.LocalMachine;
                        break;
                    case RegistryHive.PerformanceData:
                        root = Registry.PerformanceData;
                        break;
                    case RegistryHive.Users:
                        root = Registry.Users;
                        break;
                    default:
                        ex = "注册表路径错误";
                        return false;
                }

                RegistryKey key = root.OpenSubKey(path, true);
                if (key == null)
                {
                    root.CreateSubKey(path);
                    key = root.OpenSubKey(path, true);
                }

                object str = key.GetValue(theKey);
                key.SetValue(theKey, theValue, ValueKind);//修改键值
                key.Flush();
                key.Close();
                ex = "";
                return true;
            }
            catch (Exception e)
            {
                ex = e.Message;
                return false;
            }
        }

        private string GetValue(RegistryHive regk, string path, string theKey, RegistryValueKind ValueKind)
        {
            try
            {
                RegistryKey key = null;
                switch (regk)
                {
                    case RegistryHive.ClassesRoot:
                        key = Registry.ClassesRoot.OpenSubKey(path, true);
                        break;
                    case RegistryHive.CurrentConfig:
                        key = Registry.CurrentConfig.OpenSubKey(path, true);
                        break;
                    case RegistryHive.CurrentUser:
                        key = Registry.CurrentUser.OpenSubKey(path, true);
                        break;
                    case RegistryHive.DynData:
                        key = Registry.DynData.OpenSubKey(path, true);
                        break;
                    case RegistryHive.LocalMachine:
                        key = Registry.LocalMachine.OpenSubKey(path, true);
                        break;
                    case RegistryHive.PerformanceData:
                        key = Registry.PerformanceData.OpenSubKey(path, true);
                        break;
                    case RegistryHive.Users:
                        key = Registry.Users.OpenSubKey(path, true);
                        break;
                    default:
                        return "注册表路径错误";
                }

                object str = key.GetValue(theKey, ValueKind);


                key.Flush();
                key.Close();
                return str.ToString();
            }
            catch
            {
                return "读取失败";
            }
        }
        #endregion

        #endregion

    }
}
