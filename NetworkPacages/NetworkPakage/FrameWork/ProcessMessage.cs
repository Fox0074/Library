using System;
using System.Linq;
using System.Reflection;


public class ProcessMessages
{
    private static Functions functions = new Functions();
    public static void ProcessMessage(Unit msg, Client client)
    {
        string MethodName = msg.Command;
        if (MethodName == "OnPing") return;

        // ищем запрошенный метод в кольце текущего уровня
        MethodInfo method = functions.GetType().GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        try
        {
            if (method == null)
            {
                throw new Exception(string.Concat("Метод \"", MethodName, "\" недоступен"));
            }

            try
            {
                // выполняем метод интерфейса
                msg.ReturnValue = method.Invoke(functions, msg.prms);
            }
            catch (Exception ex)
            {
                throw ex.InnerException ?? ex;
            }

            // возвращаем ref и out параметры
            msg.prms = method.GetParameters().Select(x => x.ParameterType.IsByRef ? msg.prms[x.Position] : null).ToArray();
        }
        catch (Exception ex)
        {
            //msg.Exception = ex;
        }
        finally
        {
            if (msg.IsSync)
            {
                // возвращаем результат выполнения запроса
                msg.IsAnswer = true;
                client.SendData(msg);
            }
            else
            {
                //if (msg.Exception != null)
                {
                    msg.IsAnswer = true;
                    client.SendData(msg);
                }
            }
        }
    }
}

