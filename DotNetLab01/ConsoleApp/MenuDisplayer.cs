using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class MenuDisplayer
    {
        public static void ShowMenu()
        {
            Console.WriteLine("Оберіть операцію:");
            Console.WriteLine("1. - Перевірити баланс.");
            Console.WriteLine("2. - Поповнити баланс.");
            Console.WriteLine("3. - Зняти кошти.");
            Console.WriteLine("4. - Перевірити наявність кредитних коштів.");
            Console.WriteLine("5. - Взяти кредит.");
            Console.WriteLine("6. - Погасити кредит.");
            Console.WriteLine("7. - Переказ за номером картки.");
            Console.WriteLine("8. - Змінити пароль.");
            Console.WriteLine("9. - Вихід у меню.");
        }
    }
}
