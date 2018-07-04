# NServiceBus.Transport.Email

[![Build status](https://ci.appveyor.com/api/projects/status/s13ig5w3kbf6pih2/branch/master?svg=true)](https://ci.appveyor.com/project/SitoCH/nservicebus-transport-email/branch/master)


## About

NServiceBus.Transport.Email is a custom transport for NServiceBus based on IMAP and SMTP. It allows you to use a regular mail server in order to deliver messages between endpoints.
Just a quick list of all the advantages:

- no need to install or configure something new
- no need to use complex analogies to explain a service bus, it's just as easy as using Outlook
- put "Exchange based ESB" or "Gmail powered ESB" in your PowerPoint slides
- Azure Service Bus may be more enterprisy, MSMQ reliable or Kafka cool but e-mails are a never ending classic
- Exchange admin center is the new way to monitor the endpoints
- Production issues? Just manually send an e-mail and everything will work again
- Delivered a message to the wrong endpoint? No problem, just forward the e-mail

## On a serious note

Do not think about using NServiceBus.Transport.Email on production, this project started as a joke between coworkers about how great Outlook could be as a queue monitor if we used e-mails instead of MSMQ.
I wanted to understand how customizable NServiceBus is and so I put together this proof on concept. For obscure reason it even passes all the official NServiceBus tests for custom transports (NServiceBus.TransportTests.Sources).
