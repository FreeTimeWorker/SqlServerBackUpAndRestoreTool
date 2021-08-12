using Chloe.SqlServer;
using System;
using System.Collections.Generic;
using System.IO;
namespace bak
{
    class Program
    {
        static void Main(string[] args)
        {
            //发布说明，
            Console.WriteLine("还原/备份(restore/backup):");
            var rep = Console.ReadLine();
            while (!(rep.ToLower() == "restore" || rep.ToLower() == "backup"))
            {
                Console.WriteLine("还原/备份(restore/backup)输入restore或backup继续:");
                rep = Console.ReadLine();
            }
            if (rep == "restore")
            {
                restore();
            }
            else if (rep == "backup")
            {
                backup();
            }
            else
            {
                Console.WriteLine("结束");
            }
            Console.ReadKey();
        }
        static void backup()
        {
            string connStr = "data source=.;user id=sa;password=ld123456a*;initial catalog=master";
            Console.WriteLine(string.Concat("默认连接字符串:", connStr));
            Console.Write("请输入连接字符串(按Enter选择默认连接字符串):");
            var newconstr = Console.ReadLine();
            if (string.IsNullOrEmpty(newconstr))
            {
                Console.WriteLine(string.Concat("当前所选择的连接字符串为:", connStr));
            }
            else
            {
                connStr = newconstr;
                Console.WriteLine(string.Concat("当前所选择的连接字符串为:", connStr));
            }
            Console.Write("请输入备份路径:");
            var dir = Console.ReadLine();
            while (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                Console.Write("输入的路径有错误,请输入备份路径:");
                dir = Console.ReadLine();
            }
            Console.Write("请输入要备份的数据库，以【 , 】分割,默认全部库，按回车确认:");
            var databaseStr = Console.ReadLine().Replace("，",",");
            var databases = databaseStr.Split(',');
            var willbackup = new List<string>();
            var uselessdatabase = new List<string>();
            if (!string.IsNullOrEmpty(databaseStr))
            {
                //备份全部数据库
                //查询每个看看数据库是否都存在
                foreach (var item in databases)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        using (MsSqlContext context = new MsSqlContext(connStr))
                        {
                            var obj = context.SqlQuery<dynamic>($"select top 1 * from sys.databases where name = '{item.Trim()}'");
                            if (obj.Count > 0)
                            {
                                willbackup.Add(item.Trim());
                            }
                            else
                            {
                                uselessdatabase.Add(item.Trim());
                            }
                        }
                    }
                }
            }
            else
            {
                using (MsSqlContext context = new MsSqlContext(connStr))
                {
                    willbackup = context.SqlQuery<string>($"select name from sys.databases where database_id > 4");
                }
            }
            Console.WriteLine(string.Concat("将备份的数据库为:", string.Join(',', willbackup)));
            if (uselessdatabase.Count > 0)
            {
                Console.WriteLine(string.Concat("以下数据库不存在将跳过:", string.Join(',', uselessdatabase)));
            }
            Console.WriteLine("是否继续(y/n):");
            var contiue = false;
            var cont = Console.ReadLine();
            while (!(cont.ToLower() == "y" || cont.ToLower() == "n"))
            {
                Console.WriteLine("是否继续(y/n):");
                cont = Console.ReadLine();
            }
            contiue = cont == "y" ? true : false;
            if (contiue)
            {
                foreach (var item in willbackup)
                {
                    string fileName = Path.Combine(dir,string.Concat(DateTime.Now.ToString("yyyy-MM-dd-hhmmssss_"), item, ".bak"));


                    try
                    {
                        using (MsSqlContext context = new MsSqlContext(connStr))
                        {
                            context.Session.CommandTimeout = int.MaxValue;
                            context.SqlQuery<dynamic>(@$"
                            BACKUP DATABASE [{item}] TO  DISK = '{fileName}'
                            WITH NOFORMAT, NOINIT, NAME = N'{item}-完整 数据库 备份', SKIP, NOREWIND, NOUNLOAD");
                        }
                        Console.WriteLine($"数据库{item}备份成功");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"数据库{item}备份失败---{ex.Message}");
                    }
                }
                Console.WriteLine($"备份执行结束");
            }
            else
            {
                Console.WriteLine("结束");
            }
        }
        static void restore()
        {
            string connStr = "data source=.;user id=sa;password=ld123456a*;initial catalog=master";
            Console.WriteLine(string.Concat("默认连接字符串:", connStr));
            Console.Write("请输入连接字符串(按Enter选择默认连接字符串):");
            var newconstr = Console.ReadLine();
            if (string.IsNullOrEmpty(newconstr))
            {
                Console.WriteLine(string.Concat("当前所选择的连接字符串为:", connStr));
            }
            else
            {
                connStr = newconstr;
                Console.WriteLine(string.Concat("当前所选择的连接字符串为:", connStr));
            }
            Console.Write("请输入bak所在路径:");
            var dir = Console.ReadLine();
            while (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                Console.Write("输入的路径有错误,请输入bak所在路径:");
                dir = Console.ReadLine();
            }
            Console.Write("请输入还原后的路径:");
            var recoverDir = Console.ReadLine();
            while (string.IsNullOrEmpty(recoverDir) || !Directory.Exists(recoverDir))
            {
                Console.Write("输入的路径有错误,请输入还原后的路径:");
                recoverDir = Console.ReadLine();
            }
            Console.WriteLine("是否覆盖(y/n):");
            var replace = false;
            var rep = Console.ReadLine();
            while (!(rep.ToLower() == "y" || rep.ToLower() == "n"))
            {
                Console.WriteLine("是否覆盖(y/n):");
                rep = Console.ReadLine();
            }
            replace = rep == "y" ? true : false;
            string[] files = Directory.GetFiles(dir);
            foreach (var item in files)
            {
                try
                {
                    using (MsSqlContext context = new MsSqlContext(connStr))
                    {
                        context.Session.CommandTimeout = int.MaxValue;
                        var headInfo = context.SqlQuery<dynamic>($"RESTORE HEADERONLY FROM DISK = '{item}'");
                        var fileInfo = context.SqlQuery<dynamic>($"RESTORE FILELISTONLY from disk= N'{item}'");
                        if (headInfo.Count < 1)
                        {
                            Console.WriteLine($"文件,{item}，还原失败");
                            continue;
                        }
                        else
                        {
                            var databaseName = headInfo[0].DatabaseName;
                            var dataName = fileInfo[0].LogicalName;
                            var logName = fileInfo[1].LogicalName;
                            string restorSql = $@"RESTORE DATABASE {databaseName} from disk= N'{item}' 
                        WITH NOUNLOAD,
                        {(replace ? "REPLACE," : "")}
                            MOVE '{dataName}' TO '{Path.Combine(recoverDir, string.Concat(databaseName, ".mdf"))}',
                            MOVE '{logName}' TO '{Path.Combine(recoverDir, string.Concat(databaseName, ".ldf"))}';";
                            Console.WriteLine($"正在还原{databaseName}");
                            context.SqlQuery<dynamic>(restorSql);
                            Console.WriteLine($"还原{databaseName}成功");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Console.WriteLine($"还原执行结束");
        }
    }
}
