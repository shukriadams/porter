# Dev

## Setup

Porter requires Porter packages to function, creating a chicken-egg dependency problem. 

- clone this repo

### Python bootstrapper

Porter includes its own bare-bones Python implementation that can be used to bootstrap itself.

- cd <your checkout>
- Run 

      python3 porter.py --install src/Porter

  Replace <python3> with whatever Python3 runtime bind you have on yourself. Porter's
  bootstrap script is known to work with Python 3.12 on Linux.

### Porter binary

An existing binary of Porter will also install itself. If you don't have one already

- download a build from https://github.com/shukriadams/porter/releases
- Porter is portable, so you can execute the binary directly. Rename/place the binary based on your local system requirements. For the purpose of this guide we assume you've named the binary `porter`.
- cd <your checkout>/src/Porter
- Run

      porter --install

