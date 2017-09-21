# WordPressAlexa
An ASP.NET Core 2.0 project for creating Alexa skills for WordPress sites
The REST API will take requests from the Amazon Alexa service, process them and return an answer for Alexa to say.

# Gettings started

## Deploy the backend
- Clone or download this repository
- Go to `appsettings.json` and change the `WordPressUri` to your site
- Pubslish the site. You'll need a valid SSL certificate for Amazon to accept your API endpoints. Easiest way is to publish to Azure since all of their subdomains (e.g. https://myalexaskill.azurewebsites.net) are covered by their wildcard certificate.

## Create the Alexa skill
