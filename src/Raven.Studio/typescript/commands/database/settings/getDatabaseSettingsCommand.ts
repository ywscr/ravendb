import commandBase = require("commands/commandBase");
import database = require("models/resources/database");
import endpoints = require("endpoints");

class getDatabaseSettingsCommand extends commandBase {

    constructor(private db: database, private refresh: boolean) {
        super();
    }

    execute(): JQueryPromise<Raven.Server.Config.SettingsResult> {
        
        const url = endpoints.global.adminConfiguration.adminConfigurationSettings;

        return this.query<Raven.Server.Config.SettingsResult>(url, null, this.db)
            .done(() => {
                if (this.refresh) {
                    this.reportSuccess("Database Settings was successfully refreshed");
                }
             })
            .fail((response: JQueryXHR) => this.reportError("Failed to load the Database Settings", response.responseText, response.statusText));
    }
}

export = getDatabaseSettingsCommand;
