# emaillm

Email Management via LLM. This is a hacky project to test out some concept. Use at your own risk.

Email is generally sensitive and secure in that password resets may allow someone to take over your account, and private data may be send and received in emails that you wouldn't want anyone to get a hold of.

The idea was to be able to process emails from multiple accounts using a local LLM, all running on a local machine so that privacy concerns (and costs of using an LLM) are negated.

It is not intended to be a product that would be used or sold, just a way for people who decide to use it to manage their emails in a more efficient and secure way.

In order to get around any security issues with regards to gaining access to email accounts, it was decided at least for the POC to just use UI automation. It was considered to use Browser automation, however that would mean each email client would need to be supported, and that would be a lot of work. So the idea was to use a desktop email client, and automate that.

## Installation

This currently only works on Windows as an experimental POC. It could evolve later.

Initially it was thought to automate the email client, we could use Appium, then it could more easily be cross-platform. However due to setup issues, the POC will be done with FlaUI as that was more simple to setup.

### FlaUI

1. Clone the repository

...


### Appium (not working yet)

1. Clone the repository
1. Install Appium globally `npm install -g appium`
1. Install the driver `appium driver install --source=npm appium-windows-driver`

...