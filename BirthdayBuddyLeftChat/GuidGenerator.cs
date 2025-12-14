namespace BirthdayBuddyLeftChat
{
    public static class GuidGenerator
    {
        /// <summary>
        /// Метод генерации для Linux (в 16 раз быстрее).
        /// Не соответствует стандарту GUID (RFC 4122).
        /// В Windows все нормально - можно пользоваться Guid.NewGuid()
        /// </summary>
        /// <returns></returns>
        public static unsafe Guid GuidRandom()
        {
            //Выделяем память на стеке
            byte* bytes = stackalloc byte[16]; //128 бит
            //Адрес
            byte* dst = bytes;

            //Вызовем Random.Next() 4 раза и соберем из них Guid.
            for (int i = 0; i < 4; i++)
            {
                *(int*)dst = Random.Shared.Next();
                //Следующие 4 байта
                dst += 4;
            }

            //Guid в .NET — это blittable тип: его бинарное представление совпадает с 16 байтами в памяти
            return *(Guid*)bytes;
        }
    }
}
