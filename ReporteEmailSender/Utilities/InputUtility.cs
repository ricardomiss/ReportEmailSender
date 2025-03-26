using System.Text;

namespace ReporteEmailSender.Utilities
{
    public static class InputUtility
    {
        public static T PromptForInput<T>(
            string prompt,
            Func<string, bool> validator,
            string errorMsg,
            Func<string, T> converter = null)
        {
            while (true)
            {
                Console.Write($"\r{prompt} ");
                int originalLeft = Console.CursorLeft;
                int originalTop = Console.CursorTop;

                string input = Console.ReadLine();

                Console.SetCursorPosition(originalLeft, originalTop);
                Console.Write(new string(' ', Console.WindowWidth - originalLeft));
                Console.SetCursorPosition(originalLeft, originalTop);
                if (validator(input))
                {
                    return converter != null ? converter(input) : (T)(object)input;
                }
            }
        }

        public static string PromptForBody()
        {
            Console.WriteLine("Escribe el cuerpo del correo:");
            var body = new StringBuilder();
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }
                body.AppendLine(line);
            }
            return body.ToString();
        }

        public static List<string> PromptForInputList(
            string prompt,
            Func<string, bool> validator,
            string errorMsg = "Entrada Invalida")
        {
            var res = new List<string>();
            while (true)
            {
                Console.WriteLine(prompt);
                if(res.Count > 0)
                {
                    Console.WriteLine("Envia un campo vacio para terminar");
                }
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    break;
                }

                if (validator != null && !validator(input))
                {
                    Console.WriteLine(errorMsg);
                    continue;
                }

                res.Add(input);
            }
            return res;
        }

        public static bool PromptForBool()
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                    default:
                        break;
                }
            }
        }

        public static List<T> SelectOptions<T>(List<T> list)
        {
            int index = 0;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Selecciona un asunto con las flechas ↑ | ↓");
                Console.WriteLine("1.Agregar 2.Editar 3.Eliminar 4.Atras");

                for (int i = 0; i < list.Count; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{i + 1} - {list[i]} <");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"{i + 1} - {list[i]}");
                    }
                }

                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        index = index == 0 ? list.Count - 1 : index - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        index = index == list.Count - 1 ? 0 : index + 1;
                        break;
                    case ConsoleKey.D1:
                        list.Add(InputUtility.PromptForInput<T>(
                            "Escribe el nuevo asunto:",
                            input => !string.IsNullOrEmpty(input),
                            "Subject invalido"));
                        break;
                    case ConsoleKey.D2:
                        list[index] = InputUtility.PromptForInput<T>(
                            "Escribe el nuevo asunto:",
                            input => !string.IsNullOrEmpty(input),
                            "Subject invalido");
                        break;
                    case ConsoleKey.D3:
                        list.RemoveAt(index);
                        break;
                    case ConsoleKey.D4:
                        return list;
                    default:
                        break;
                }

            }

        }
    }
}
