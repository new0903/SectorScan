

## Быстрый старт

### Тестовый клиент на JavaScript

Для проверки работы сервера можно использовать код в браузере

eventSource = new EventSource('https://localhost:2345/api/Stations/1/250099?latS=38&latE=81&lonS=20&lonE=180'); 
eventSource.onmessage = (event) => {
 console.log(event.data);     
 if (event.data.includes('[DONE]')) {eventSource.close()}
};


по этому url https://localhost:2345/api/Stations будет полное сканирование

Есть GRPC запросы в проекте ConsoleAppGRPCTest можно посмотреть как это работает

## Обзор

Загружает все станции в бд


Для сканирования использовал квадраты 6x6. Если в секторе больше 300 станций делим квадрат на 2 и снова и снова пока станций  не будет меньше 300.


Для ускорения я использовал прокси. 


Вот такой результат возвращаю

    public record QueryResult(
        string OperatorCode,//код оператора
        NetworkStandard Network,//тип сети 0=2g, 1=3g,2=4g,3=5g
        int CountAdded, // количество найденных станций
        int CountSectorsScaned, // количество отсканированных квадратов
        int CountSectors, // количество квадратов не отсканированных, значение может увеличеваться
        string Message, // meta инфа
        bool isDone=false) // это последнее сообщение
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow; // дата 
    }


Для сохранения станций использую EFCore.BulkExtensions


