# HandleEvents

Long running storage queue triggered function to handle events from a source
The function will wait for a time period before restarting itself by posting a message back to queue


Need to set up an Azure storage queue to read from/post to (configure in local.settings.json)