# Alexitech

ASP.NET MVC application which integrates Logitech Harmony Hubs to be controlled via Amazon's Alexa-end devices, including the Amazon Echo.

Background
----------

This application leverages several existing components to allow Alexa to control your Harmony Hub. The [HarmonyHub](https://github.com/hdurdle/harmony) and [agsXMPP](https://www.nuget.org/packages/agsXMPP/) libraries provide communication with the Harmony Hub. Amazon's [Alexa Skills Kit](https://developer.amazon.com/public/solutions/alexa/alexa-skills-kit) does all the voice-to-intent and mapping.

This application provides an intent map for the Alexa Skills Kit and handles requests. It also provides an authentication process when the skill is activited in the Alexa app.

Communicates is performed directly to the Harmony Hub using an unofficial API that was reversed engineered. The protocol is the same as is used by the Harmony mobile application. Although there is an official API from Logitech for the Harmony platform, it's currently in a closed beta.

There is a live production version of the application which has been submitted for certification with Logitech. It will almost certainly be rejected for violating rules regarding secure authentication. It may only be possible to pass certification if Logitech themselves submitted the skill.

Current Features
----------------

- Starting (and switching) activities
- Playing and pausing
- Pressing any button in the current activity
- Shutting off all devices
- A "listen" mode which allows repeated commands

Planned Features
----------------

- Macro recording and playback
- Better volume control

Prerequisites
-------------

If you want to deploy this application, you will need the following:

- Windows with IIS 7 (or later) installed
- ASP.NET 4.5.2
- SQL Server 2005 (or later). The free Express edition version will work.
- An [Amazon developer](https://developer.amazon.com/) account

Getting Started
---------------

When you issue voice commands to Alexa, the voice data is streamed to a Amazon server. Commands mapped to a custom skill are handled via a web service. The service calls are issued from the cloud server, so the application must be accessible from the internet.

Using your Amazon developer account, you must create an Alexa Skills Kit application. Any Alexa device tied to the Amazon account will have access to the skill. The configuration settings necessary are listed below in the "Alexa Skill Configuration" section.

The application needs to be authenticated in order to communicate with your Harmony Hub. The authentication process can be initiated from within the Alexa app's skill section. Search for the name of your skill, open the details screen for the skill, and tap the "authenticate" link.

Application Configuration
-------------------------

Some values need to be set in the web.config file to configure the application:

- "AuthUrl" is the URL provided when you configure the Alexa Skill for authentication
- The "Manager" connection string needs to have the correct authentication values set for your database.

Database Setup
--------------

The "database.sql" file contains a script to build the database schema used by the application. Simply run the script inside a blank database.

Alexa Skill Configuration
-------------------------

This section lists the necessary configuration values when creating the Alexa Skill. The subsections correlate to the configuration sections on the Skill management screen.

##### Skill Information

- Skill Type: Custom Interaction Model
- Name: [Your choice]
- Invoication Name: [Your choice, but I recommend "remote"]

##### Interaction Model 

- Intent Schema: [Contents of the "intent schema.txt" file]
- Custom Slot Types: [Contents of the "intent custom slot types.txt" file]
- Sample Utterances: [Contents of the "intent sample utterances" file]

##### Configuration 

- Endpoint: [Base URL]/Do
- Account Linking: Yes
- Authorization URL: [Base URL]/Auth
- Client Id: alexa-skill
- Authorization Grant Type: Implicit Grant
- Privacy Policy URL: [Base URL]/Help/Privacy

Copy the "Redirect URL" value to the application's web.config file.

##### SSL Certificate 

Follow the instructions for creating a self-signed certificate, if necessary.

##### Publishing Information 

These values are only necessary if you are planning to submit the skill for certification.

##### Privacy & Compliance 

These values are only necessary if you are planning to submit the skill for certification.

Credit Where Credit is Due
--------------------------

Both @hdurdle and @jterrace have done amazing work on their respective Harmony projects to make this application possible. 
