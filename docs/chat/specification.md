<!--
Authors: Rajdeep, Zeeshan, Rajat
Roll numbers: 112301045, 112301040, 112301044
-->

# Specification

Specification for the chat module.

## Overview

Here is an overview of what things look like.

- All the people log into their systems.
- One person starts a meeting---they are the host.
- The other people provide an identifier to join the meeting---the host's IP
address for example.
- This creates a chatroom with all the participants, and one additional special
participant---our AI called ChAI (short for 'Chat AI').
- The host's computer acts as the server.
- The attendees computers are clients.
- The server manages the chat.
- Chats do **not** persist across sessions.

## Features

### General features

- Messages.
    - Users can send messages in the chatroom, with textual and emoji content.
    - The UI shows the list of messages, along with the sender names, their profile photos and
    timestamps.
- Editing messages.
    - Users can edit their messages, and this explicitly shows an "edited" tag
    with the message.
    - The number of edits is also displayed in the UI
- Deleting messages.
    - Users can delete their own messages (delete for everyone, no delete for
    me).
    - The UI shows something like "message deleted by <user>" in case a message
    is delete.
- Replies.
    - Users can reply to messages.
    - The UI shows the reply as well as the message that is being replied to.
    - Users can go to the message being replied to by clicking on the same.
- Threads.
    - Users have an option to reply to a message in a thread.
    - If the thread does not currently exist, it is created with the reply as
    the first message in it.
    - If the thread exists, the message is added at the bottom of the thread.
    - Users can edit and delete messages in the thread as well, and the
    behavior here is very similar to the behavior in the main chatroom.
    - There can only be one level of threading
- Reactions
    - Users can react to messages with a particular emoji.
    - Reaction counts are accumulated and displayed in the UI.
- Mentions
    - You can mention a particular user or a bunch of users in a message by mentioning their username.
    - `@all` is a special mention.
    - If a message mentions you, that is specially highlighted for you.

### AI features

You can chat with an AI bot called `ChAI` -- it is a virtual participant in every meeting.

- You can prompt the AI by mentioning `ChAI`.
- It can answer questions relevant to the chat. Some example prompts are:
    - How many messages have been sent in the chat so far?
    - What did foo say about bar (related to something sent in the chat)
    - Summarize the entire chat in bullet points.
- The AI answers in the form of a message, replying to the prompt that it
is answering.
- Messages sent by the AI can be replied to in order to prompt the AI again.

## Stretch Goals

- Pinning messages.
    - Users can pin one message in the room.
    - This shows up as a line at the top of the chatroom, clicking on which
    takes users to the message.
    - Maybe we can have something where only some users can pin a message.
- Formatting and styling in messages.
    - This can be done markdown style.
    - `_italics_` or `*italics*` for _italics_, `**bold**` for **bold**, etc.

