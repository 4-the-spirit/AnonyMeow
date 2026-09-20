// One-off seed script (dev-only) that populates the local backend with a realistic-looking
// dataset (several communities, users, posts of varied types, comments, votes, reactions) via
// its dev-only /api/dev/token auth bypass, for portfolio screenshot/video capture. Not part of
// the test suite. Run manually: node backend/seed/seed-portfolio-data.mjs
const API = 'https://localhost:7225'

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0' // dev self-signed cert only

async function req(method, path, token, body) {
  const res = await fetch(API + path, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(`${method} ${path} -> ${res.status}: ${text}`)
  }
  if (res.status === 204) return null
  return res.json().catch(() => null)
}

async function makeUser(oid, username, displayName, avatarSeed) {
  const { token } = await req('POST', `/api/dev/token?oid=${encodeURIComponent(oid)}`)
  await req('POST', '/api/auth/complete-profile', token, {
    username,
    displayName,
    avatarSeed: avatarSeed || username,
  })
  return { token, username, displayName }
}

async function main() {
  console.log('Creating users...')
  const users = {
    alex: await makeUser('seed-alex', 'alex_moss', 'Alex Moss'),
    sam: await makeUser('seed-sam', 'sam_riley', 'Sam Riley'),
    jordan: await makeUser('seed-jordan', 'jordan_lee', 'Jordan Lee'),
    taylor: await makeUser('seed-taylor', 'taylor_finch', 'Taylor Finch'),
    morgan: await makeUser('seed-morgan', 'morgan_reyes', 'Morgan Reyes'),
  }
  console.log(
    'Users:',
    Object.values(users)
      .map((u) => u.username)
      .join(', '),
  )

  const communities = [
    {
      name: 'cat-photos',
      description: 'A cozy corner for sharing cat photos, stories, and everything feline.',
      rulesText: '1. Be kind.\n2. Cats only.\n3. No spam.',
      creator: users.alex,
      members: [users.sam, users.jordan, users.taylor, users.morgan],
    },
    {
      name: 'indie-games',
      description: 'Discover, discuss, and share indie games you love.',
      rulesText: '1. No piracy links.\n2. Mark spoilers.',
      creator: users.sam,
      members: [users.alex, users.jordan],
    },
    {
      name: 'home-cooking',
      description: 'Recipes, techniques, and kitchen wins (and fails).',
      rulesText: '1. Include ingredients in text posts.\n2. Be nice about substitutions.',
      creator: users.jordan,
      members: [users.alex, users.taylor, users.morgan],
    },
    {
      name: 'houseplants',
      description: 'Grow your plant family — care tips, ID help, and plant flexing.',
      rulesText: null,
      creator: users.taylor,
      members: [users.sam, users.morgan],
    },
    {
      name: 'book-club',
      description: 'What are you reading? Come talk about it.',
      rulesText: '1. Spoiler-tag anything past chapter 1.',
      creator: users.morgan,
      members: [users.alex, users.jordan, users.taylor],
    },
  ]

  console.log('\nCreating communities...')
  for (const c of communities) {
    await req('POST', '/api/communities', c.creator.token, {
      name: c.name,
      description: c.description,
      rulesText: c.rulesText,
    })
    for (const m of c.members) {
      await req('POST', `/api/communities/${c.name}/join`, m.token)
    }
    console.log(`  c/${c.name} (by ${c.creator.username}, ${c.members.length} members joined)`)
  }

  const postsByCommunity = {
    'cat-photos': [
      {
        author: users.alex,
        type: 'Text',
        title: 'My cat finally tolerates the new kitten',
        bodyMarkdown:
          "Six months of hissing and we're finally at the *grudging co-existence* stage. Small victories. 🐱",
      },
      {
        author: users.sam,
        type: 'Question',
        title: 'Best way to trim claws without losing a finger?',
        bodyMarkdown: 'My cat turns into a blender the moment she sees the clippers. Tips?',
      },
      {
        author: users.jordan,
        type: 'Poll',
        title: 'Best cat toy?',
        pollOptions: ['Laser pointer', 'Cardboard box', 'Feather wand', 'Crinkle ball'],
      },
      {
        author: users.taylor,
        type: 'Link',
        title: 'Great read on cat body language',
        url: 'https://www.aspca.org/pet-care/cat-care/common-cat-behavior-issues/cat-body-language',
      },
    ],
    'indie-games': [
      {
        author: users.sam,
        type: 'Text',
        title: 'Just finished a 40-hour indie RPG and I have thoughts',
        bodyMarkdown:
          'The pacing in the back half falls apart but the soundtrack alone makes it worth it.',
      },
      {
        author: users.alex,
        type: 'Poll',
        title: 'Which genre needs more indie love?',
        pollOptions: ['Immersive sim', 'Tactics/SRPG', 'Metroidvania', 'Point-and-click'],
      },
      {
        author: users.jordan,
        type: 'Question',
        title: 'Recommend me something cozy for a rainy weekend?',
        bodyMarkdown: 'Nothing stressful. Low stakes. Good vibes only.',
      },
    ],
    'home-cooking': [
      {
        author: users.jordan,
        type: 'Text',
        title: 'Weeknight pasta that actually tastes like it took effort',
        bodyMarkdown:
          'Brown butter, garlic, anchovy, lemon zest, tons of parm. 20 minutes, zero regrets.',
      },
      {
        author: users.morgan,
        type: 'Question',
        title: 'How do you keep rice from turning to mush in a rice cooker?',
        bodyMarkdown: "Every batch comes out gluey lately and I can't figure out why.",
      },
      {
        author: users.alex,
        type: 'Poll',
        title: 'Sunday dinner: what are we making?',
        pollOptions: ['Roast chicken', 'Homemade pizza', 'Big pot of chili', 'Stir fry'],
      },
    ],
    houseplants: [
      {
        author: users.taylor,
        type: 'Text',
        title: 'My monstera finally pushed a new fenestrated leaf!',
        bodyMarkdown: 'Two years of mediocre growth and it finally decided to show off.',
      },
      {
        author: users.sam,
        type: 'Question',
        title: 'Yellow leaves on my pothos — overwatered or underwatered?',
        bodyMarkdown: 'Bottom two leaves went yellow this week, rest of the plant looks fine.',
      },
    ],
    'book-club': [
      {
        author: users.morgan,
        type: 'Text',
        title: "This month's pick: a slow-burn sci-fi mystery",
        bodyMarkdown: 'No spoilers past chapter 5 in this thread please!',
      },
      {
        author: users.taylor,
        type: 'Poll',
        title: 'What should we read next month?',
        pollOptions: ['A classic we all skipped in school', 'Translated fiction', 'A thriller'],
      },
    ],
  }

  console.log('\nCreating posts...')
  /** @type {Record<string, {id: string, author: any, communityName: string}[]>} */
  const createdPosts = {}
  for (const [communityName, posts] of Object.entries(postsByCommunity)) {
    createdPosts[communityName] = []
    for (const p of posts) {
      const body = {
        type: p.type,
        title: p.title,
        bodyMarkdown: p.bodyMarkdown ?? null,
        url: p.url ?? null,
        imageUrl: null,
        pollOptions: p.pollOptions ?? null,
      }
      const created = await req(
        'POST',
        `/api/communities/${communityName}/posts`,
        p.author.token,
        body,
      )
      createdPosts[communityName].push({ id: created.id, author: p.author, communityName })
      console.log(`  [${communityName}] "${p.title}" (${p.type}) by ${p.author.username}`)
    }
  }

  console.log('\nCasting votes and reactions from other members...')
  const allUsers = Object.values(users)
  for (const posts of Object.values(createdPosts)) {
    for (const post of posts) {
      const voters = allUsers.filter((u) => u.username !== post.author.username)
      for (const voter of voters) {
        // Mostly upvotes, occasional downvote, occasional skip, for natural-looking spread.
        const roll = Math.random()
        if (roll < 0.6) {
          await req('PUT', `/api/posts/${post.id}/vote`, voter.token, { value: 1 })
        } else if (roll < 0.75) {
          await req('PUT', `/api/posts/${post.id}/vote`, voter.token, { value: -1 })
        }
      }
      const reactionEmojis = ['👍', '❤️', '😂', '🔥']
      for (const voter of voters.slice(0, 2)) {
        const emoji = reactionEmojis[Math.floor(Math.random() * reactionEmojis.length)]
        await req(
          'PUT',
          `/api/posts/${post.id}/reactions/${encodeURIComponent(emoji)}`,
          voter.token,
        )
      }
    }
  }

  console.log('\nAdding comments (with a few nested replies)...')
  const commentsByPostTitle = {
    'My cat finally tolerates the new kitten': [
      { author: users.sam, text: 'The slow blink truce is real. Give it another month.' },
      { author: users.jordan, text: 'Update us when they nap in a pile together 🥹' },
    ],
    'Best way to trim claws without losing a finger?': [
      { author: users.alex, text: 'Towel burrito method. Works every time for mine.' },
    ],
    "Just finished a 40-hour indie RPG and I have thoughts": [
      { author: users.jordan, text: 'Which one? I need more soundtrack recs.' },
    ],
    'Weeknight pasta that actually tastes like it took effort': [
      { author: users.alex, text: 'Anchovy is doing a lot of quiet heavy lifting there, respect.' },
      { author: users.taylor, text: 'Adding this to the rotation, thank you.' },
    ],
    'My monstera finally pushed a new fenestrated leaf!': [
      { author: users.morgan, text: 'The patience it takes. Beautiful leaf though!' },
    ],
  }

  for (const posts of Object.values(createdPosts)) {
    for (const post of posts) {
      // Need the title back; refetch minimal post to map comments (cheap, dev-only script).
      const full = await req('GET', `/api/posts/${post.id}`, post.author.token)
      const comments = commentsByPostTitle[full.title]
      if (!comments) continue
      let firstCommentId = null
      for (const [idx, c] of comments.entries()) {
        const created = await req('POST', `/api/posts/${post.id}/comments`, c.author.token, {
          bodyMarkdown: c.text,
          parentCommentId: idx === 1 ? firstCommentId : null,
        })
        if (idx === 0) firstCommentId = created.id
      }
    }
  }

  console.log('\nSending a couple of friend requests and DMs for realism...')
  await req('POST', '/api/users/sam_riley/friend-requests', users.alex.token)
  await req('POST', '/api/users/alex_moss/friend-requests/accept', users.sam.token)
  await req('POST', '/api/users/jordan_lee/friend-requests', users.taylor.token)

  const convo = await req('POST', '/api/conversations', users.alex.token, {
    username: 'sam_riley',
  })
  if (convo?.id) {
    await req('POST', `/api/conversations/${convo.id}/messages`, users.alex.token, {
      body: 'Hey! Saw your indie RPG post, what was the soundtrack again?',
    })
    await req('POST', `/api/conversations/${convo.id}/messages`, users.sam.token, {
      body: "It's the one from that pixel-art tactics game, I'll dig up the link!",
    })
  }

  console.log('\nSeed complete.')
}

main().catch((err) => {
  console.error('\nSeed failed:', err.message)
  process.exit(1)
})
