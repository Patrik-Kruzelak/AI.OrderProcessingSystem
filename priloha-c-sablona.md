# AI Workflow Dokumentácia

**Meno:** Patrik Kruželák

**Dátum začiatku:** 06.12.2025

**Dátum dokončenia:** 07.12.2025

**Zadanie:** Backend

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Claude Code:** 5 hodín 
- [ ] **ChatGPT:** 20 min

**Celkový čas vývoja (približne):** 6 hodín

---

## 2. Zbierka Promptov

> 💡 **Tip:** Kopíruj presný text promptu! Priebežne dopĺňaj po každej feature.

### Prompt #1: Setup CLAUDE.md

**Nástroj:** [ ChatGPT ]  
**Kontext:** [ Setup CLAUDE.md ]

**Prompt:**
```
This solution is divided into the following projects, where I want to maintain the following logic (if needed, add it directly into the CLAUDE.md file to ensure these rules are respected):

AI.OrderProcessingSystem.Common

Contains logic that needs to be shared between multiple projects, e.g. constants, commands, events, enums, etc., which are required by both the WebApi and the Worker.

This project can be referenced by all other projects.

AI.OrderProcessingSystem.Dal

Contains database logic such as models, DbContext, migration files, etc.

This project can be referenced by all projects except AI.OrderProcessingSystem.Common.

AI.OrderProcessingSystem.WebApi

Contains the Web API logic of the application.

Can reference everything except AI.OrderProcessingSystem.Worker and AI.OrderProcessingSystem.CronJob.

AI.OrderProcessingSystem.Worker

Contains primarily the asynchronous logic of the application.

Can reference everything except AI.OrderProcessingSystem.WebApi and AI.OrderProcessingSystem.CronJob.

AI.OrderProcessingSystem.CronJob

Will contain cron jobs that need to be executed.

Can reference everything except AI.OrderProcessingSystem.WebApi and AI.OrderProcessingSystem.Worker.

If settings that must not be shared are required (e.g. passwords), create the file:

D:\SIGP notebook\Claude Code Project\Configuration\secrets.json


This file must be added to .gitignore. Also create and modify as needed a file named secrets_template.json, which will serve only as a template so that all secrets can later be created on the server.

If other settings are required, such as URLs, create the file:

D:\SIGP notebook\Claude Code Project\Configuration\instance.json


This file will be normally committed.

When the application starts, these configuration files must be loaded together and form a single configuration in the application.

A Docker configuration will also need to be created for the application, and I want it to be located in:

D:\SIGP notebook\Claude Code Project


When implementing, try to use best practices.

If I have missed anything important, please add it to avoid issues in further prompts.
```

**Výsledok:**  
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy

**Čo som musel upraviť / opraviť:**
```
Neupravoval som nič, len som použil niektoré „nastavenia", ktoré chýbali v pôvodnom CLAUDE.md
```

**Poznámky / Learnings:**
```
Je potrebné si viac preštudovať, ako správne písať CLAUDE.md a aké pravidlá je tam správne dávať.
```
### Prompt #2: B: Zadanie - Backend Čast 1 - enhance-initial

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 1 - enhance-initial ]

**Prompt:**
```
/enhance-initial.md

In this project, I need to create a PostgreSQL database that will be set up via Docker, which means we must also create a Dockerfile that will be added to the Git repository.

Using this Dockerfile, I would also like to run the Web API project.

Next, we need to create the following tables:

- User has the following fields: id, name (max length 100), email (max length 100 and unique), password string
- Product has the following fields: id, name (string, max length 100), description (string), price (number ≥ 0), stock (number ≥ 0), created_at (timestamp)
- Order has the following fields: id, user_id, total (number ≥ 0), status enum (pending, processing, completed, expired), items schema id (primary key), product_id, quantity (number > 0), price (number > 0), created_at (timestamp), updated_at (timestamp)

These database models should be created in the project AI.OrderProcessingSystem.Dal, where the database context will also be added. I would like to use Entity Framework in this project. This project should also contain all migration files.

For application usage, I will also need to create an Admin user, which should be created immediately via seed, and at the end provide the password.

Once we have the database and the Admin user ready, we will need endpoints in AI.OrderProcessingSystem.WebApi. The first thing we should implement is the Login REST API – Check user credentials (email, password) and if correct, return JWT token. Use best practices when generating the token.

Next, for the existing database/models, we need to create endpoints in the Web API project:

- User: Create CRUD REST API for this module. Validate input DTOs; if invalid, return 400.
- Product: Create CRUD REST API for this module. Validate input DTOs; if invalid, return 400.
- Order: Create CRUD REST API for this module. Validate input DTOs. The rules are defined in the schema.

Additional requirements:
- Endpoints must be secured with a JWT Bearer token (result of the Login REST API).
- Correctly handle error return states (400 Bad Request, 401 Unauthorized, 404 Not Found, 500 Internal Server Error, etc.).
- Include OpenAPI/Swagger documentation.
- Integration tests (minimum 5 test cases).
- Use PostgreSQL DB. Run PostgreSQL in Docker and initialize it using the docker compose file. Include the docker compose file in the Git repository.
- Include a DB upgrade mechanism in the final solution. It must contain some form of DB upgrade scripts or DB upgrade code.
- Also include initial seed data in the DB; it can be part of the upgrade mechanism as well.
- In README.md, document how to run the DB upgrade tool and how to start the service.

If necessary, adjust the type of existing projects that are currently created as console applications. The console applications were generated only as an initial skeleton.
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval žiadne úpravy.
```

**Poznámky:**
```
Viac špecifikovať knižnice, ktoré chcem, aby sa používali, napr. pre unit testy, a nekopírovať zadanie doslovne. Napríklad Integration tests (minimum 5 test cases) vyhodnotil, že stačí práve 5 testov, a spravil ich pre jeden controller.
```
### Prompt #3: B: Zadanie - Backend Čast 1 - generate-prp

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 1 - generate-prp]

**Prompt:**
```
/generate-prp INITIAL.md
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny, hodnotenie bolo 8,5 z dôvodu, že som skomplikoval settingy (-1 bod, chcel som secrets.json a následné mergovanie) a bolo potrebné vytvoriť Admin používateľa (-0,5), ktorý sa seedoval
```

**Poznámky:**
```
```
### Prompt #4: B: Zadanie - Backend Čast 1 - execute-prp

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 1 - execute-prp]

**Prompt:**
```
/execute-prp order-processing-system-api
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Trvalo to približne 10 minút
```
### Prompt #5: B: Zadanie - Backend Čast 1 - spustenie dockera a aplikacie

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 1 - spustenie dockera a aplikacie]

**Prompt:**
```
start Docker, run the migrations, provide the Admin password, and start the application so I can test it
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny, skôr Claude Code musel robiť nejaké zmeny, ktoré urobil počas execute-prp v dockere
```

**Poznámky:**
```
Trvalo to približne 14,5 min, v tomto momente som mal vyčerpaných 88 %
```
### Prompt #6: B: Zadanie - Backend Čast 1 - vytvorenie dalsich testov

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 1 - vytvorenie dalsich testov]

**Prompt:**
```
create integration and unit tests for all existing controllers
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Tento prompt som použil len aby som videl, ako sa správa Claude Code po vyčerpaní tokenov.
```
### Prompt #7: B: Zadanie - Backend Čast 2 - Event-Driven

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - Event-Driven ]

**Prompt:**
```
I need to add an Event-Driven Architecture. I have experience with MassTransit with AWS, but now I would like to try something new. I have a choice between RabbitMQ, Kafka, or Redis. The requirements are that it should be runnable in Docker, events will be published from the Web API and processed by a worker, and I will also need an event bus to function properly. Which of these: RabbitMQ, Kafka, or Redis is the most suitable, and why?
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
```
### Prompt #8: B: Zadanie - Backend Čast 2 - enhance-initial

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - enhance-initial ]

**Prompt:**
```
/enhance-initial.md

Adding Event-Driven Architecture + Background Processing

We will need to create a new table Notifications with the following columns: id, order_id, eventType (enum: OrderCreated, OrderCompleted, OrderExpired), message (string), is_email_sent (bool), created_at. We do not need CRUD endpoints for this table.

All asynchronous operations, including handling events from the event bus, should be implemented in the AI.OrderProcessingSystem.Worker project.

I want to use (RabbitMQ, Kafka, or Redis) and an event bus, which needs to be run in the existing Dockerfile. I also want to run AI.OrderProcessingSystem.Worker and AI.OrderProcessingSystem.CronJob there.

Order Creation Flow:
1. User creates an order via POST /api/orders. (WebApi)
2. Order is saved in the DB with status=pending.
3. OrderCreated event is published. (From WebApi)
4. Worker handles the OrderCreated event asynchronously: (Worker)
- Update order status: pending → processing.
- Simulate payment processing (5 second delay).
- For 50% of cases, update status → completed and publish OrderCompleted.
- For 50% of cases, leave status as processing.

Notifications: (this will be done in consumers in the Worker)
- Create a Notifications table in the database.
- When OrderCompleted is published:
--- Log a fake email to the console.
--- Save a notification in the DB (audit trail).
- When OrderExpired is published:
--- Save a notification in the DB (audit trail).

I will also need to run a cron job, which will be implemented in AI.OrderProcessingSystem.CronJob, and should work as follows:
- CronJob runs every 60 seconds.
- Finds orders with status=processing older than 10 minutes.
- Updates status → expired.
- Publishes OrderExpired event.

All constants, such as 50%, 10 minutes, 60 seconds, etc., should be added to \Configuration\instance.json.

If necessary, update the README.md file again.

When implementing, try to use best practices.

If necessary, adjust the type of existing projects that are currently created as console applications. The console applications were generated only as an initial skeleton.
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval žiadne úpravy
```

**Poznámky:**
```
Nezabúdať pridávať informáciu o UTC pre db záznamy, malo by sa to pridať do CLAUDE.md
```
### Prompt #9: B: Zadanie - Backend Čast 2 - generate-prp

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - generate-prp]

**Prompt:**
```
/generate-prp INITIAL.md
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Trvalo to približne 8 minút, z nejakého dôvodu dokončil len 4/10 úloh, následne som použil ďalší prompt, aby som pokračoval
```
### Prompt #10: B: Zadanie - Backend Čast 2 - problem s testami

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - problem s testami]

**Prompt:**
```
try fix all tests for controllers
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Po prekročení tokenov v Claude Code som dal možnosť /resume, ale z nejakého dôvodu vyhodnotil, že testy sú v poriadku, a neotestoval ich, tak ich dal upraviť až teraz.
```
### Prompt #11: B: Zadanie - Backend Čast 2 - execute-prp - kontrola

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - execute-prp - kontrola]

**Prompt:**
```
can you show me result from prompt: /execute-prp event-driven-architecture
```

**Výsledok:**  
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy 

**Úpravy:**
```
Zabudol nastavovať processing, zle nastavil konfiguračné hodnoty a vytvoril navyše OrderExpiredConsumer, co nie je až taký problém
```

**Poznámky:**
```
```
### Prompt #12: B: Zadanie - Backend Čast 2 - enhance-initial fix pre part 2

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - enhance-initial fix pre part 2 ]

**Prompt:**
```
/enhance-initial.md

Identified Issues & Required Corrections

In the current flow, I found several missing or incorrectly implemented parts. The following adjustments are required:

Expected Flow:

1. User creates an order via POST /api/orders – correct
2. Order is saved to the database with status = pending – correct
3. OrderCreated event is published – correct
4. OrderCreated event processing - This part is implemented incorrectly.
It should be handled entirely in OrderCreatedConsumer with the following logic:
- Update order status: pending - processing
- Simulate payment processing (5-second delay) - the 5 seconds must be configurable in instance.json
- For 50% of processed orders: change status - completed
- publish OrderCompleted event
- For the remaining 50%, do not modify the status - the order stays in processing, isn't need to publish nothing
5. OrderCompleted event publishing - This event must be published only in OrderCreatedConsumer when the status becomes completed. (For 50% of processed orders: change status - completed from point 4)
6. Notification handling:
- Fake email logging is currently implemented incorrectly - must be moved to OrderCompletedConsumer
- Saving the notification into DB is fine 
7. CRON job behavior: 
- Runs every 60 seconds –  correct
- Finds processing orders older than 10 minutes – correct
- Updates their status to expired - OrderExpiredEvent must not be published anymore, so: remove the event and consumer associated with it
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval žiadne úpravy
```

**Poznámky:**
```
```
### Prompt #13: B: Zadanie - Backend Čast 2 - generate-prp part 2 fix

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - generate-prp part 2 fix]

**Prompt:**
```
/generate-prp INITIAL.md
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)  
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy 

**Úpravy:**
```
Nepotreboval som žiadne zmeny, hodnotenie bolo 9
```

**Poznámky:**
```
```
### Prompt #14: B: Zadanie - Backend Čast 2 - execute-prp - fix

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - execute-prp - fix]

**Prompt:**
```
/execute-prp event-driven-architecture-fix
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Trvalo to približne 10 minút
```
### Prompt #15: B: Zadanie - Backend Čast 2 - fixovanie bugov

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - fixovanie bugov]

**Prompt:**
```
for orderprocessing-cronjob I see error logs:
fail: AI.OrderProcessingSystem.CronJob.Services.OrderExpiryService[0]
      Error occurred while checking for expired orders
      System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.EntityFrameworkCore.Relational, Version=8.0.11.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.
      
      File name: 'Microsoft.EntityFrameworkCore.Relational, Version=8.0.11.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
         at AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext.<>c.<OnModelCreating>b__21_1(EntityTypeBuilder`1 entity)
         at Microsoft.EntityFrameworkCore.ModelBuilder.Entity[TEntity](Action`1 buildAction)
         at AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext.OnModelCreating(ModelBuilder modelBuilder) in /src/AI.OrderProcessingSystem.Dal/Data/OrderProcessingDbContext.cs:line 33
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizer.Customize(ModelBuilder modelBuilder, DbContext context)
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.CreateModel(DbContext context, IConventionSetBuilder conventionSetBuilder, ModelDependencies modelDependencies)
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
         at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
         at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
         at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine.<>c__DisplayClass2_0.<RealizeService>b__0(ServiceProviderEngineScope scope)
         at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
         at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
         at Microsoft.EntityFrameworkCore.DbContext.get_ContextServices()
         at Microsoft.EntityFrameworkCore.DbContext.get_Model()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.get_EntityType()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.CheckState()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.get_EntityQueryable()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.System.Linq.IQueryable.get_Provider()
         at System.Linq.Queryable.Where[TSource](IQueryable`1 source, Expression`1 predicate)
         at AI.OrderProcessingSystem.CronJob.Services.OrderExpiryService.CheckAndExpireOrdersAsync(CancellationToken cancellationToken) in /src/AI.OrderProcessingSystem.CronJob/Services/OrderExpiryService.cs:line 55
         at AI.OrderProcessingSystem.CronJob.Services.OrderExpiryService.ExecuteAsync(CancellationToken stoppingToken) in /src/AI.OrderProcessingSystem.CronJob/Services/OrderExpiryService.cs:line 39
fail: AI.OrderProcessingSystem.CronJob.Services.OrderExpiryService[0]
      Error occurred while checking for expired orders
      System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.EntityFrameworkCore.Relational, Version=8.0.11.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.
      
      File name: 'Microsoft.EntityFrameworkCore.Relational, Version=8.0.11.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
         at AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext.<>c.<OnModelCreating>b__21_1(EntityTypeBuilder`1 entity)
         at Microsoft.EntityFrameworkCore.ModelBuilder.Entity[TEntity](Action`1 buildAction)
         at AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext.OnModelCreating(ModelBuilder modelBuilder) in /src/AI.OrderProcessingSystem.Dal/Data/OrderProcessingDbContext.cs:line 33
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizer.Customize(ModelBuilder modelBuilder, DbContext context)
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.CreateModel(DbContext context, IConventionSetBuilder conventionSetBuilder, ModelDependencies modelDependencies)
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
         at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
         at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
         at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine.<>c__DisplayClass2_0.<RealizeService>b__0(ServiceProviderEngineScope scope)
         at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
         at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
         at Microsoft.EntityFrameworkCore.DbContext.get_ContextServices()
         at Microsoft.EntityFrameworkCore.DbContext.get_Model()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.get_EntityType()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.CheckState()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.get_EntityQueryable()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.System.Linq.IQueryable.get_Provider()
         at System.Linq.Queryable.Where[TSource](IQueryable`1 source, Expression`1 predicate)
         at AI.OrderProcessingSystem.CronJob.Services.OrderExpiryService.CheckAndExpireOrdersAsync(CancellationToken cancellationToken) in /src/AI.OrderProcessingSystem.CronJob/Services/OrderExpiryService.cs:line 55
         at AI.OrderProcessingSystem.CronJob.Services.OrderExpiryService.ExecuteAsync(CancellationToken stoppingToken) in /src/AI.OrderProcessingSystem.CronJob/Services/OrderExpiryService.cs:line 39

 and in orderprocessing-worker is error:
 fail: MassTransit.ReceiveTransport[0]
      R-FAULT rabbitmq://rabbitmq/order-created-queue 01000000-0006-ac13-126f-08de3511b6cd AI.OrderProcessingSystem.Common.Events.OrderCreatedEvent AI.OrderProcessingSystem.Worker.Consumers.OrderCreatedConsumer(00:00:00.3977285)
      System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.EntityFrameworkCore.Relational, Version=8.0.11.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.
      
      File name: 'Microsoft.EntityFrameworkCore.Relational, Version=8.0.11.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
         at AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext.<>c.<OnModelCreating>b__21_1(EntityTypeBuilder`1 entity)
         at Microsoft.EntityFrameworkCore.ModelBuilder.Entity[TEntity](Action`1 buildAction)
         at AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext.OnModelCreating(ModelBuilder modelBuilder) in /src/AI.OrderProcessingSystem.Dal/Data/OrderProcessingDbContext.cs:line 33
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizer.Customize(ModelBuilder modelBuilder, DbContext context)
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.CreateModel(DbContext context, IConventionSetBuilder conventionSetBuilder, ModelDependencies modelDependencies)
         at Microsoft.EntityFrameworkCore.Infrastructure.ModelSource.GetModel(DbContext context, ModelCreationDependencies modelCreationDependencies, Boolean designTime)
         at Microsoft.EntityFrameworkCore.Internal.DbContextServices.CreateModel(Boolean designTime)
         at Microsoft.EntityFrameworkCore.Internal.DbContextServices.get_Model()
         at Microsoft.EntityFrameworkCore.Infrastructure.EntityFrameworkServicesBuilder.<>c.<TryAddCoreServices>b__8_4(IServiceProvider p)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine.<>c__DisplayClass2_0.<RealizeService>b__0(ServiceProviderEngineScope scope)
         at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
         at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService(IServiceProvider provider, Type serviceType)
         at Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService[T](IServiceProvider provider)
         at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
         at Microsoft.EntityFrameworkCore.DbContext.get_ContextServices()
         at Microsoft.EntityFrameworkCore.DbContext.get_Model()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.get_EntityType()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.CheckState()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.get_EntityQueryable()
         at Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1.System.Linq.IQueryable.get_Provider()
         at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ExecuteAsync[TSource,TResult](MethodInfo operatorMethodInfo, IQueryable`1 source, Expression expression, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ExecuteAsync[TSource,TResult](MethodInfo operatorMethodInfo, IQueryable`1 source, LambdaExpression expression, CancellationToken cancellationToken)
         at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync[TSource](IQueryable`1 source, Expression`1 predicate, CancellationToken cancellationToken)
         at AI.OrderProcessingSystem.Worker.Consumers.OrderCreatedConsumer.Consume(ConsumeContext`1 context) in /src/AI.OrderProcessingSystem.Worker/Consumers/OrderCreatedConsumer.cs:line 41
         at MassTransit.DependencyInjection.ScopeConsumerFactory`1.Send[TMessage](ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22
         at MassTransit.DependencyInjection.ScopeConsumerFactory`1.Send[TMessage](ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/DependencyInjection/DependencyInjection/ScopeConsumerFactory.cs:line 22
         at MassTransit.Middleware.ConsumerMessageFilter`2.MassTransit.IFilter<MassTransit.ConsumeContext<TMessage>>.Send(ConsumeContext`1 context, IPipe`1 next) in /_/src/MassTransit/Middleware/ConsumerMessageFilter.cs:line 48
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny pre túto chybu, ale vznikla nová
```

**Poznámky:**
```
Pri tomto prompte nefungoval /resume, musel som ho ráno vytvoriť znova
```
### Prompt #16: B: Zadanie - Backend Čast 2 - fixovanie bugov

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - fixovanie bugov]

**Prompt:**
```
now for the orderprocessing-worker I have error fail: Microsoft.EntityFrameworkCore.Database.Connection[20004]
      An error occurred using the connection to database 'orderprocessing' on server 'tcp://localhost:5432'.
fail: Microsoft.EntityFrameworkCore.Query[10100]
      An exception occurred while iterating over the results of a query for context type 'AI.OrderProcessingSystem.Dal.Data.OrderProcessingDbContext'.
      System.InvalidOperationException: An exception has been raised that is likely due to a transient failure.
       ---> Npgsql.NpgsqlException (0x80004005): Failed to connect to [::1]:5432
       ---> System.Net.Sockets.SocketException (99): Cannot assign requested address
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
V tomto momente už bežala celá aplikácia
```
### Prompt #17: B: Zadanie - Backend Čast 2 - fixovanie bugov

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - fixovanie bugov]

**Prompt:**
```
I can see in the logs that the SELECT query in the cron job is being executed, but no order is being updated to expired
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Toto bol asi chyták v zadaní
```
### Prompt #18: B: Zadanie - Backend Čast 2 - fixovanie bugov

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - fixovanie bugov]

**Prompt:**
```
I was mistaken, in the cron job, I need to publish the OrderExpired event, and only then should the consumer save the notification
```

**Výsledok:**  
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy  

**Úpravy:**
```
Pridal odoslanie emailu do expired consumera
```

**Poznámky:**
```
Toto bola moja chyba, pôvodne to tam vygeneroval, ale keď som sa zameral na finálny flow, tak som na tento event zabudol
```
### Prompt #19: B: Zadanie - Backend Čast 2 - fixovanie bugov

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - fixovanie bugov]

**Prompt:**
```
For OrderExpiredEvent, I don’t need to send a fake email, it should only be sent for OrderCompleted, which is implemented correctly
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
Nepotreboval som žiadne zmeny
```

**Poznámky:**
```
Sám sa rozhodol, že okopíruje logiku z OrderCompletedConsumer, dôvod pravdepodobne bol, že sa resetol context, v ktorom bolo definované, ako sa má správať OrderExpiredConsumer, takže asi bude lepšie písať vždy celú logiku, ako to má fungovať
```
### Prompt #20: B: Zadanie - Backend Čast 2 - README.md update

**Nástroj:** [ Claude Code ]  
**Kontext:** [ B: Zadanie - Backend Čast 2 - README.md update]

**Prompt:**
```
Update the README.md if necessary
```

**Výsledok:**  
[ ] ✅ Fungoval perfektne (first try)

**Úpravy:**
```
```

**Poznámky:**
```
Toto by som asi volal po každej väčšej zmene
```

---
## 3. Problémy a Riešenia 

> 💡 **Tip:** Problémy sú cenné! Ukazujú ako riešiš problémy s AI.

### Problém #1: _________________________________

**Čo sa stalo:**
```
Pre prompt /execute-prp event-driven-architecture sa vykonalo len 4/10 naplánovaných krokov, nepamätám sa, že by mi ponúkol Claude nejakú možnosť, chcel som to skontrolovať spätne, ale problém je, že ctrl+o (história) je skratka vo VS na inú funkciu.
```

**Prečo to vzniklo:**
```
Nie som si istý, lebo som sa nedostal k histórii chatu
```

**Ako som to vyriešil:**
```
Požiadal som o kontrolu promptu /execute-prp event-driven-architecture a Claude zistil, kde skončil, a ponúkol pokračovanie
```

**Čo som sa naučil:**
```
Treba lepšie čítať chat, asi je lepšie pre väčšie zmeny potvrdzovať každú zmenu manuálne
```

**Screenshot / Kód:** [ ] Priložený

---

### Problém #2: _________________________________

**Čo sa stalo:**
```
Konflikt medzi verziami EF vo Workerovi, CronJobe a Dal projektoch
```

**Prečo:**
```
Nedefinoval som Claudovi, aké verzie má použiť, keďže som RabbitMQ nepouží val, tak som nevedel o tomto probléme
```

**Riešenie:**
```
Poskytol som Claudovi chybný log a on sa postaral o úpravu verzií
```

**Learning:**
```
Asi by bolo dobré definovať v CLAUDE.md, aby sa kontrolovali verzie medzi projektami v danej solution
```

### Problém #3: _________________________________

**Čo sa stalo:**
```
Worker pracoval s lokálnou databázou namiesto databázy v containeri
```

**Prečo:**
```
Databáza bola vytvorená a používaná vo web api cez prvý PRP a Worker cez druhé, síce som definoval, že sa všetko má rozbehať v dockeri, ale výslovne som mu nepovedal, ktorú databázu má použiť.
```

**Riešenie:**
```
Požiadal som Claude Code o kontrolu, prečo je problém s databázou, a on identifikoval zlý connection string
```

**Learning:**
```
Informácie o tom, aké databázy a ktoré servicy/projekty ich používajú, bolo asi vhodné pridať do CLAUDE.md, aby sa na to nezabudlo
```
### Problém #4: _________________________________

**Čo sa stalo:**
```
Nepublishoval sa OrderExpired
```

**Prečo:**
```
Zabudol som, ako sa má správať OrderExpired, pôvodne tam bol vygenerovaný, čo bolo správne, ale ja som v finále pozrel len Expected Flow a tam už nebol spomenutý, takže toto bola moja chyba.
```

**Riešenie:**
```
Požiadal som Claude Code o pridanie tohto eventu a consumera
```

**Learning:**
```
Asi by bolo lepšie si napísať vlastný očakávaný, kde by som si odchytil nedostatky špecifikácie (čo za normálnych okolností robím)
```
### Problém #4: _________________________________

**Čo sa stalo:**
```
Nejaké drobné chyby v implementácii
```

**Prečo:**
```
Očakával som, že si to pamätá
```

**Riešenie:**
```
Postačuje používať základné prompty na fixovanie nezrovnalostí v kóde
```

**Learning:**
```
Písať komplexnejší prompt s viac informáciami alebo to vysvetľovať ako novému človeku na projekte
```

## 4. Kľúčové Poznatky

### 4.1 Čo fungovalo výborne

**1.**
```
Prvá časť zadania fungovala perfektne, vytvorenie databázy, dockera, JWT tokenov, controllerov aj testov
```

**2.**
```
Druhá časť bola tiež veľmi dobre vytvorená, čo sa týkalo RabbitMq, CronJobu a úpravy databázy. Finálny flow bol taktiež veľmi dobre vytvorený a veľmi rýchlo.
```
---

### 4.2 Čo bolo náročné

**1.**
```
Asi to vyčerpanie tokenov a následné čakanie na reset, nechcel som nič programovať manuálne a chcel som vidieť, či si Claude poradí so všetkým. Nebolo to náročné, skôr taký pocit, že som bloknutý, ale to sa dá vyriešiť kúpou vyššej verzie.
```

---

### 4.3 Best Practices ktoré som objavil

**1.**
```
Predpríprava CLAUDE.md, pre prvú časť som upravoval CLAUDE.md a asi aj preto tam bol lepší výsledok po PRP. Možno keby som strávil znova čas úpravou CLAUDE.md, tak by som odchytil možné problémy s knižnicami a databázou.
```

**2.**
```
Pre "simple" prompty je lepšie poskytovať reálne čo najviac informácií o tom, čo je zle, a ak vieme možné riešenie, tak aj popis riešenia
```

**3.**
```
Ak je možnosť, tak problém riešiť asi po menších častiach, PRP funguje fakt dobre, ale môže obsahovať až príliš veľa zmien, ktoré by sa mali reálne aj skontrolovať
```

**4.**
```
Ak chceme, aby Claude použil nejakú konkrétnu implementáciu, napr. Consumer chceme s definíciou, tak by bolo fajn pripraviť vzorový príklad, aby sa inšpiroval, ako chceme, aby finálna implementácia vyzerala
```
---

### 4.4 Moje Top 3 Tipy Pre Ostatných

**Tip #1:**
```
Nepodceňovať CLAUDE.md a jeho prípravu a kontrolu
```

**Tip #2:**
```
Písať prompty čo najpresnejšie, aj keď to môže vyzerať ako jednoduchá zmena, tak sa môže stať, že Claude použije niečo, čo tam nemá byť, lebo sa inšpiruje inou podobnou implementáciou v projekte
```

**Tip #3:**
```
Radšej potvrdzovať každú zmenu s tým, že si to reálne aj prečítame, ako povoliť robiť všetky zmeny, preto by som asi radšej odporúčal riešiť problémy po častiach, než urob polku aplikácie cez jeden prompt
```
**Tip #4:**
```
Možno by stálo za zváženie používať Claude Code cez separovaný cmd mimo Visual studia, dôvod je, že skratky, ktoré používa Claude Code, používa aj Visual Studio na iné funkcie, napr. ctrl + t je vo VS search a v Claude Code by mal stav aktuálneho plánu
```
---

## 6. Reflexia a Závery

### 6.1 Efektivita AI nástrojov

**Ktorý nástroj bol najužitočnejší?** Claude code

**Prečo?**
```
Chcel som sa s ním naučiť robiť a porovnať s Copilotom, ktorý používam
```

**Ktorý nástroj bol najmenej užitočný?** neskúšal viacero nástrojov

---

### 6.2 Najväčšie prekvapenie
```
Veľmi sa mi páčilo, že Claude Code viem otvoriť rovno nad celým priečinkom, kde môžem mať aj viac solutions, s týmto bol problém v Copilote.
Páči sa mi, ako Claude rieši reálny problém, že si buildne aplikáciu, spustí testy, ak je potrebné, tak otestuje aj cron job atď., s čím som sa zatiaľ nestretol a je to veľmi užitočné a reálne to ušetrí veľa času.
```

---

### 6.3 Najväčšia frustrácia
```
Asi to, že výsledný program závisí od môjho vstupu, ktorý môžem ovplyvňovať len ak mi dá Claude možnosť, a neviem, kedy tá možnosť nastane.
```

---

### 6.4 Najväčší "AHA!" moment
```
Pri druhej časti, keď z daného zadania ani AI nebolo schopné spraviť, čo sa očakávalo, takže stále závisí od vstupu, aby sme dostali čo najlepší výsledok.
```

---

### 6.5 Čo by som urobil inak
```
Prešiel by som si celé zadanie znova, vytvoril možno activity diagram a ten použil ako vstup do PRP, zo zvedavosti, ako by si poradil s takýmto vstupom.
```

### 6.6 Hlavný odkaz pre ostatných
```
Určite to vyskúšať, je to VEĽKÁ zmena oproti ChatGPT a Copilotom, a ušetrí to viac reálneho času
```
