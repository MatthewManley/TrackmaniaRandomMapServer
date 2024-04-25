## Bugs

- Sometimes votes stick around (after AT?)
  - This can probably just be solved by always resetting votes on map start
- Clicking Force buttons before map starts causes RMT to soft lock


## Features

- HIGH PRIORITY - Allow setting the hub map (voting? / admins only?)
- HIGH PRIORITY - Post RMT results/updates to discord
- Allow setting RMT difficulty
  - Win can be set to AT / Gold / Silver / Bronze
  - Skip can be set to Gold / Silver / Bronze / Finish
- Allow setting RMT duration
- If all players vote for something, don't wait for a player to hit the force button
  - Extend the player voted server message to include total players
- Ohio branding on templates
- Widget that links to the discord?
- MOTD?
  - Advertise 6pm RMTs
  - Advertise Discord
  - Spotlight users
  - Encourage entries for hub maps
- Implement logic for switching ManiaExchange hosts
  - Automatic failover?
- Improve logic for downloading maps
- Implement storage driver pattern
  - Currently files are uploaded over xml rpc
  - Direct access over docker shares
  - SFTP access if controller is on different server than the tm server
  - Other ideas?

## Project Tasks

- CI/CD pipeline in github actions (build and push to docker hub)
- Documentation on how to run the controller
  - Example docker compose?
- Documentation on how to contribute to the project
