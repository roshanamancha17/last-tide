# Marlow server

A small Node server (no packages to install) that generates Marlow's replies with Groq's free tier.
It keeps the API key out of the game, limits use, and checks every reply before the game sees it.
If it can't answer, the game uses Marlow's pre-written lines.

## Run it locally

1. Create a free API key at https://console.groq.com/keys
2. Copy `.env.example` to `.env` in this folder and paste your key after `GROQ_API_KEY=`
3. Check that `GROQ_MODEL` is listed on your Groq account's Limits page
4. Run `npm run dev`, then press Play in Unity

Never commit `.env` or share your key. Run the tests with `npm test`; they use a fake AI service and no key.
