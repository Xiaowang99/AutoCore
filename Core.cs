using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using AutoCore.CoreMode;

namespace AutoCore
{
    /// <summary>
    /// 默认的测试逻辑运行的测试平台
    /// 自动化测试框架的核心算法类
    /// </summary>
    public class Core
    {
        public static Core _Core = null;

        /// <summary>
        /// 单实例构造
        /// </summary>
        /// <returns></returns>
        public static Core GetInstance()
        {
            if (_Core == null)
                return new Core();
            else
                return _Core;
        }

        /// <summary>
        /// 获取加载的逻辑文件的测试集用于反射操作.
        /// </summary>
        public Assembly asmPlugin = null;

        /// <summary>
        /// 加载的测试逻辑名称
        /// </summary>
        public string logicFileName = null;

        /// <summary>
        /// 需要执行的测试逻辑具体信息
        /// </summary>
        public List<LogicInfo> lstRunLogic = new List<LogicInfo>();

        /// <summary>
        /// 加载的dll通过反射得到的所有测试逻辑
        /// </summary>
        public List<LogicInfo> lstLogicInfo = new List<LogicInfo>();

        /// <summary>
        /// 调试栈深度
        /// </summary>
        int DecFrame = 0;

        /// <summary>
        /// 单个测试逻辑执行测试数据
        /// </summary>
        /// <param name="AwLogic"></param>
        /// <param name="data"></param>
        public void TestLogic_Raw(ILogic AwLogic, Dictionary<string, string> data)
        {
            DecFrame += 2;
            var errMsg = string.Empty;
            try
            {
                //测试逻辑加载测试数据
                AwLogic.AwLogic(data);
                Console.WriteLine("=======测试逻辑OK=======");
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            finally
            {
                try
                {
                    //进行单个用例的测试环境恢复工作
                    AwLogic.RestoreEnv(data);
                }
                catch(Exception ex1)
                {
                    Console.WriteLine("测试逻辑" + AwLogic.GetType().FullName + "在测试用例" + data["用例名称"] + "恢复环境失败");
                    Console.WriteLine("失败原因： " + ex1.Message);
                }

                //调试栈深度
                DecFrame -= 2;
            }

            if (!string.IsNullOrEmpty(errMsg))
                throw new Exception(errMsg);
        }

        /// <summary>
        /// 将测试逻辑与测试集进行绑定
        /// </summary>
        private void BindLogicAndCluster()
        {
            //初始化测试集链表
            var lstAllTestCluster = new List<ITestCluster>();

            //遍历所有的测试逻辑以初始化所有的测试集
            foreach (var logicInfo in lstLogicInfo)
            {
                //这边加载的时候如果出现测试逻辑为null则跳过
                if (logicInfo == null)
                    continue;

                //判断当前的测试链表是否已经添加了该测试逻辑的测试集
                var testCluster = lstAllTestCluster.FirstOrDefault<ITestCluster>(src =>
                {
                    return src.GetType().FullName.Equals(logicInfo.logic.TestClusterType.FullName);
                });

                //如果没有找到新的测试逻辑的测试集就new一个测试集
                if (testCluster == null)
                {
                    try
                    {
                        //通过测试逻辑程序集实例化该测试集
                        testCluster = this.asmPlugin.CreateInstance(logicInfo.logic.TestClusterType.FullName, true) as ITestCluster;
                        lstAllTestCluster.Add(testCluster);
                        logicInfo.logic.iTestCluster = testCluster;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("未能加载测试集{0},{1}",logicInfo.logic.TestClusterType.FullName, ex.Message) );
                    }
                }
                else
                {
                    //绑定测试集
                    logicInfo.logic.iTestCluster = testCluster;
                }
            }
            lstLogicInfo.Sort();
        }

        /// <summary>
        /// 加载所选的测试逻辑(debug模式)
        /// </summary>
        /// <param name="arrILogic"></param>
        public void LoadTestCase(params ILogic[] arrILogic)
        {
            lstLogicInfo.Clear();
            this.logicFileName = string.Empty;
            //在debug模式下不需要从程序集中加载测试逻辑
            this.asmPlugin = null;

            //遍历所有的测试逻辑添加到测试逻辑链表
            foreach (var iLogic in arrILogic)
            {
                //如果该测试逻辑为null则跳过
                if (iLogic == null)
                    continue;

                //从测试逻辑中加载所属的逻辑文件与程序集
                this.logicFileName = string.IsNullOrEmpty(this.logicFileName) ? iLogic.GetType().FullName : this.logicFileName;
                this.asmPlugin = this.asmPlugin == null ? iLogic.CurrAssembly : this.asmPlugin;

                //如果有相同的测试逻辑时则跳过不作处理
                var logicInfo = lstLogicInfo.FirstOrDefault<LogicInfo>(src =>
                {
                    return src.logic.GetType().FullName.Equals(iLogic.GetType().FullName);
                });

                //在测试逻辑链表中未能找到该测试数据则进行实例化处理
                if (logicInfo == null)
                {
                    logicInfo = new LogicInfo(iLogic);
                    lstLogicInfo.Add(logicInfo);
                }
            }

            //绑定测试逻辑与测试集合
            BindLogicAndCluster();
        }

        /// <summary>
        /// 通过加载测试逻辑的dll来反射出所有的测试逻辑与测试集
        /// </summary>
        /// <param name="fileName"></param>
        void LoadTestCase(string fileName)
        {
            lstLogicInfo.Clear();
            try
            {
                this.logicFileName = fileName;
                //这种方式加载程序集是直接加载文件.
                //this.asmPlugin = Assembly.LoadFile(fileName);
                byte[] assemblyInfo = File.ReadAllBytes(this.logicFileName);
                this.asmPlugin = Assembly.Load(assemblyInfo);

                //遍历程序集中的所有测试逻辑
                foreach (var item in asmPlugin.GetTypes())
                {
                    //如果程序集中有继承ILogic的类,则就是测试逻辑.
                    if (item.GetInterface(typeof(ILogic).ToString()) == null||!item.IsClass)
                        continue;

                    //如果在测试逻辑链表中找到该测试逻辑则跳过
                    var logicInfo = lstLogicInfo.FirstOrDefault<LogicInfo>(src =>
                    {
                        return src.logic.GetType().FullName.Equals(item.FullName);
                    });

                    //未找到该测试逻辑则new测试逻辑加入测试链表中
                    if (logicInfo == null && !item.FullName.Contains(" "))
                    {
                        try
                        {
                            //实例化该测试逻辑
                            var iLogic = asmPlugin.CreateInstance(item.FullName, true) as ILogic;
                            if (iLogic == null)
                                throw new Exception("初始化该用例失败");
                            logicInfo = new LogicInfo(iLogic);
                            lstLogicInfo.Add(logicInfo);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("加载测试逻辑失败：{0},{1}", item.FullName, ex.Message));
                        }
                    }
                }

                //绑定测试逻辑与测试集合
                BindLogicAndCluster();
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("LoadTestCase(string fileName): 加载失败 {0}", e.Message));
            }
        }

        /// <summary>
        /// 从测试数据中找到相应的用例编号
        /// </summary>
        /// <param name="data">测试数据</param>
        /// <param name="Key">用例编号</param>
        /// <param name="DefaultValue">没有找到则返回"_Un_Know_"</param>
        /// <returns></returns>
        public string GetDictValue(Dictionary<string, string> data, string Key, string DefaultValue = "_Un_Know_")
        {
            if (data.ContainsKey(Key))
                return data[Key];
            return DefaultValue;
        }

        /// <summary>
        /// 通过用例编号链表来输出应该绑定的测试逻辑与测试集
        /// </summary>
        /// <param name="lstCaseNo"></param>
        /// <param name="lstTestCluster"></param>
        /// <param name="lstRunLogic"></param>
        private void GetCurrNeedRunTestCluster(List<string> lstCaseNo, out List<ITestCluster> lstTestCluster,out List<LogicInfo> lstRunLogic)
        {
            lstTestCluster = new List<ITestCluster>();
            lstRunLogic = new List<LogicInfo>();

            //遍历所有的用例名
            foreach (var caseNo in lstCaseNo)
            {
                LogicInfo logicInfo = null;
                try
                {
                    //查找是否包含用例编号的逻辑
                    logicInfo = this.lstLogicInfo.FirstOrDefault((logicInfoSrc) =>
                    {
                        return logicInfoSrc.logic.Data().FirstOrDefault(data =>
                        {
                            return this.GetDictValue(data, "用例编号").Equals(caseNo);
                        }) != null;
                    });

                    //没有找到该用例编号的逻辑则实例化并绑定到测试逻辑链表
                    if (logicInfo != null)
                    {
                        //将逻辑添加到逻辑列表中
                        if (lstRunLogic.FirstOrDefault<LogicInfo>(src => { return src.logic.GetType().FullName.Equals(logicInfo.logic.GetType().FullName); }) == null)
                        {
                            lstRunLogic.Add(logicInfo);
                            //添加测试逻辑
                            var testCluster = lstTestCluster.FirstOrDefault<ITestCluster>(src =>
                            {
                                return src.GetType().FullName.Equals(logicInfo.logic.TestClusterType.FullName);
                            });

                            //未找到该测试集则加入需要绑定的测试集链表中
                            if (testCluster == null)
                                lstTestCluster.Add(logicInfo.logic.iTestCluster);
                        }
                    }
                    else
                    {
                        throw new Exception("未找到该测试用例编号");
                    }
                }
                catch (Exception ex)
                {
                    //一般这里是不会有错的,如果有错误则在之前的绑定那一步就报错了.
                    throw new Exception(string.Format("GetCurrNeedRunTestCluster： {0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// 输出同一个测试集下面的测试逻辑中在测试编码链表中存在的测试数据
        /// </summary>
        /// <param name="lstLogic"></param>
        /// <param name="lstCaseNo"></param>
        /// <returns></returns>
        private List<Dictionary<string, string>> FindCaseData(List<LogicInfo> lstLogic, List<string> lstCaseNo)
        {
            List<Dictionary<string,string>> lstData = new List<Dictionary<string,string>>();
            foreach (var runLogic in lstLogic)
            {
                foreach(var data in runLogic.logic.Data())
                {
                    if(lstCaseNo != null &&
                      lstCaseNo.IndexOf(this.GetDictValue(data, "用例编号"))!=-1)
                          lstData.Add(data);
                }
            }
            return lstData;
        }

        /// <summary>
        /// 根据测试用例编号来获取执行测试逻辑
        /// </summary>
        /// <param name="lstCaseNo"></param>
        public void RunTestCase(List<string> lstCaseNo)
        {
            //测试用例开始执行
            List<ITestCluster> lstTestCluster;
            List<LogicInfo> lstRunLogic;

            this.GetCurrNeedRunTestCluster(lstCaseNo, out lstTestCluster, out lstRunLogic);
            foreach (var testCluster in lstTestCluster)
            {
                try 
                {
                    string clusterName = testCluster.GetType().FullName;
                    var lstCurrRunLogic = lstRunLogic.FindAll(src=>{return src.logic.TestClusterType.FullName.Equals(clusterName);});

                    try {
                       //建立测试集预置条件
                        if(this.SetupTestClusterScripts(testCluster,this.FindCaseData(lstCurrRunLogic,lstCaseNo)))
                        {
                            //执行测试逻辑
                            foreach(var runLogic in lstCurrRunLogic)
                            {
                                try
                                {
                                    try
                                    {
                                        if(this.SetupLogicScripts(runLogic.logic,lstCaseNo,clusterName))
                                            this.ExecLogic(runLogic.logic, lstCaseNo);
                                    }finally
                                    {
                                        if(!this.RestoreLogicScripts(runLogic.logic, lstCaseNo,clusterName))
                                            //执行测试逻辑
                                            ;
                                    }
                                }catch
                                {
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if(!this.RestoreClusterScripts(testCluster))
                        {
                            Console.WriteLine("测试集：{0}环境恢复失败，可能对后续用例有影响");
                        }
                    }
                }catch
                {}
            }
        }

        /// <summary>
        /// debug模式的测试执行
        /// </summary>
        /// <param name="arrILogic"></param>
        /// <returns></returns>
        public bool ExecuteLogic(params ILogic[] arrILogic)
        {
            //装载逻辑
            this.LoadTestCase(arrILogic);
            List<string> lstTestCaseNo = new List<string>();
            foreach (var iLogic in arrILogic)
            {
                if (iLogic == null)
                    continue;
                foreach (var data in iLogic.Data())
                {
                    var caseNo = this.GetDictValue(data, "用例编号");
                    if (!string.IsNullOrWhiteSpace(caseNo))
                        lstTestCaseNo.Add(caseNo);
                }
            }
            this.RunTestCase(lstTestCaseNo);
            return true;
        }
        
        /// <summary>
        /// 加载测试逻辑程序集的方式来执行所有的测试用来
        /// </summary>
        /// <param name="logicFilePath"></param>
        /// <returns></returns>
        public bool ExEcuteLogic(string logicFilePath)
        {
          
            this.LoadTestCase(logicFilePath);
            List<string> lstTestCaseNo = new List<string>();
            foreach (var iLogicInfo in this.lstLogicInfo)
            {
                if (iLogicInfo == null)
                    continue;
                foreach (var data in iLogicInfo.logic.Data())
                {
                    var caseNo = this.GetDictValue(data, "用例编号");
                    if (!string.IsNullOrWhiteSpace(caseNo))
                        lstTestCaseNo.Add(caseNo);
                }
            }
            this.RunTestCase(lstTestCaseNo);
            return true;
        }

        /// <summary>
        /// 在同一个测试集的测试数据中在用例链表中有记录则执行
        /// </summary>
        /// <param name="iLogic"></param>
        /// <param name="lstCaseNo"></param>
        private void ExecLogic(ILogic iLogic, List<string> lstCaseNo)
        {
            Console.Write("开始测试逻辑的运行");

            foreach (var data in iLogic.Data())
            {
                var caseNo = this.GetDictValue(data, "用例编号");
                var caseTitle = this.GetDictValue(data, "用例名称");
                if (lstCaseNo != null && lstCaseNo.IndexOf(caseNo) == -1)
                    continue;

                string LogicName = iLogic.GetType().FullName;
                Dictionary<string, string> Data = data;
                string TestClusterName = iLogic.TestClusterType.FullName;

                try
                {
                    Console.WriteLine("测试用例所在的测试集:{0}", TestClusterName);
                    Console.WriteLine("开始测试用例测试:测试用例编号:{0}", caseNo);
                    Console.WriteLine("                :测试用例标题:{0}", caseTitle);
                    Console.WriteLine("                :测试逻辑名称:{0}", LogicName);
                    {
                        iLogic.AwLogic(data);
                        Console.WriteLine("停止测试用例测试，测试结果为：OK");
                    }
                }
                catch (Exception ex)
                {

                }
                finally
                {
 
                }
            }
        }

        /// <summary>
        /// 执行测试集的初始化工作
        /// </summary>
        /// <param name="testCluster"></param>
        /// <param name="lstCurRunLogicData"></param>
        /// <returns></returns>
        private bool SetupTestClusterScripts(ITestCluster testCluster, List<Dictionary<string, string>> lstCurRunLogicData)
        {
            Dictionary<string, string>[] TestCases = lstCurRunLogicData.ToArray();
            bool SetupTestClusterScripsRes = true;
            try
            {
                if (!testCluster.SetupScripts())
                {
                    SetupTestClusterScripsRes = false;
                    throw new Exception("执行配置脚本返回值为: False!");
                }
            }
            catch
            {

            }
            finally
            {
                
            }
            return SetupTestClusterScripsRes;
        }

        /// <summary>
        /// 恢复测试集的环境
        /// </summary>
        /// <param name="testCluster"></param>
        /// <returns></returns>
        private bool RestoreClusterScripts(ITestCluster testCluster)
        {
            bool RestoreClusterScriptsRes = true;
            try
            {
                testCluster.RestoreScripts();
            }
            catch
            {
                RestoreClusterScriptsRes = false;
                throw new Exception("恢复环境失败");
            }
            finally
            {
 
            }
            return RestoreClusterScriptsRes;
        }

        /// <summary>
        /// 执行单个测试逻辑
        /// </summary>
        /// <param name="iLogic"></param>
        /// <param name="lstCaseNo">所有的测试用例编号链表</param>
        /// <param name="clusterName"></param>
        /// <returns></returns>
        private bool SetupLogicScripts(ILogic iLogic, List<string> lstCaseNo, string clusterName)
        {
            bool SetupLogicScriptsRes = true;
            string TestClusterName = clusterName;
            string LogicName = iLogic.GetType().FullName.ToString();

            Dictionary<string,string>[] TestCaseData = iLogic.Data().Where(data=>
                {
                    return lstCaseNo.IndexOf(this.GetDictValue(data, "用例编号")) != -1;
                }).ToArray();

            try
            {
                //这儿是预处理测试逻辑里面的预处理
                if (!iLogic.SetupScripts())
                {
                    SetupLogicScriptsRes = false;
                    throw new Exception("测试逻辑预置条件失败");
                }
            }
            catch
            {

            }
            finally
            {
 
            }
            return SetupLogicScriptsRes;
        }

        /// <summary>
        /// 恢复测试逻辑的环境
        /// </summary>
        /// <param name="iLogic"></param>
        /// <param name="lstCaseNo"></param>
        /// <param name="clusterName"></param>
        /// <returns></returns>
        private bool RestoreLogicScripts(ILogic iLogic, List<string> lstCaseNo, string clusterName)
        {
            bool RestoreLogicScriptsRes = true;
            var TestClusterNmae = clusterName;
            var LogicName = iLogic.GetType().FullName;
            Dictionary<string,string>[] TestCaseData = iLogic.Data().Where(data=>
                {
                    return lstCaseNo.IndexOf(this.GetDictValue(data, "测试用例"))!= -1;
                }).ToArray();

            Console.WriteLine("恢复本次测试逻辑预置条件:{0}", iLogic.GetType());

            try
            {
                if (!iLogic.RestoreScripts())
                {
                    RestoreLogicScriptsRes = false;
                    throw new Exception("恢复预置条件返回值为FALSE");
                }
            }
            catch
            {
                Console.WriteLine("测试逻辑恢复脚本失败");
            }
            finally
            {
 
            }
            return RestoreLogicScriptsRes;
        }
    }
}
