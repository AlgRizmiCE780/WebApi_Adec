               +----------------------+
               |      API Gateway     |
               |  (ASP.NET Core Web)  |
               +----------+-----------+
                          |
        +-----------------+-----------------+
        |                 |                 |
+-------v-------+ +-------v--------+ +------v--------+ +--------v-------+
|  UserService  | |    Service_1   | | PaymentService| |   Service_2    |
| ASP.NET Core  | | ASP.NET Core   | | ASP.NET Core  | | ASP.NET Core   |
| Web API       | | Web API        | | Web API       | | Web API        |
| EF Core + SQL | | EF Core + SQL  | | EF Core + SQL | | EF Core + SQL  |
+---------------+ +----------------+ +---------------+ +----------------+
        |                 |                 |                 |
        +-----------------+-----------------+-----------------+
                          |
                 +--------v---------+
                 |  Message Broker  |
                 |   (RabbitMQ /    |
                 |    Azure Service)|
                 +-----------------+
