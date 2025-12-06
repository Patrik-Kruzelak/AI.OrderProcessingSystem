## FEATURE:

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

## EXAMPLES:

[Provide and explain examples that you have in the `examples/` folder]

## DOCUMENTATION:

[List out any documentation (web pages, sources for an MCP server like Crawl4AI RAG, etc.) that will need to be referenced during development]

## OTHER CONSIDERATIONS:

[Any other considerations or specific requirements - great place to include gotchas that you see AI coding assistants miss with your projects a lot]
