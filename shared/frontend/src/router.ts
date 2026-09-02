import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import SignedOutView from './views/SignedOutView.vue'
import { useAuth } from './composables/useAuth'
import {
  isQuizzesCoursesPath,
  quizzesCoursesRoutesReady,
  registerQuizzesCoursesRoutes,
} from './federation/quizzesCoursesRemote'
import {
  isMiniGamesPath,
  miniGamesRoutesReady,
  registerMiniGamesRoutes,
} from './federation/miniGamesRemote'
import {
  chatDiscussionRoutesReady,
  isChatDiscussionPath,
  registerChatDiscussionRoutes,
} from './federation/chatDiscussionRemote'
import {
  isQuestsAchievementsPath,
  questsAchievementsRoutesReady,
  registerQuestsAchievementsRoutes,
} from './federation/questsAchievementsRemote'
import {
  isLeaderboardAnalyticsPath,
  leaderboardAnalyticsRoutesReady,
  registerLeaderboardAnalyticsRoutes,
} from './federation/leaderboardAnalyticsRemote'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/index.html', redirect: '/' },
    { path: '/login', name: 'login', component: LoginView },
    {
      path: '/login.html',
      redirect: (to) => ({ path: '/login', query: to.query, hash: to.hash }),
    },
    { path: '/signed-out', name: 'signed-out', component: SignedOutView },
  ],
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async (to) => {
  if (to.meta.requiresAuth) {
    try {
      if (!(await useAuth().ensureAuthenticated())) {
        return { name: 'signed-out', query: { returnUrl: to.fullPath } }
      }
    } catch {
      return { name: 'home' }
    }
  }

  if (!isQuizzesCoursesPath(to.path) || quizzesCoursesRoutesReady()) {
    if (!isMiniGamesPath(to.path) || miniGamesRoutesReady()) {
      if (!isChatDiscussionPath(to.path) || chatDiscussionRoutesReady()) {
        if (!isQuestsAchievementsPath(to.path) || questsAchievementsRoutesReady()) {
          if (!isLeaderboardAnalyticsPath(to.path) || leaderboardAnalyticsRoutesReady()) {
            return true
          }

          if (to.name === 'leaderboard-analytics-unavailable') {
            return true
          }

          try {
            await registerLeaderboardAnalyticsRoutes(router)
            return { path: to.path, query: to.query, hash: to.hash, replace: true }
          } catch {
            return { path: to.path, query: to.query, hash: to.hash, replace: true }
          }
        }

        if (to.name === 'quests-achievements-unavailable') {
          return true
        }

        try {
          await registerQuestsAchievementsRoutes(router)
          return { path: to.path, query: to.query, hash: to.hash, replace: true }
        } catch {
          return { path: to.path, query: to.query, hash: to.hash, replace: true }
        }
      }

      if (to.name === 'chat-discussion-unavailable') {
        return true
      }

      try {
        await registerChatDiscussionRoutes(router)
        return { path: to.path, query: to.query, hash: to.hash, replace: true }
      } catch {
        return { path: to.path, query: to.query, hash: to.hash, replace: true }
      }
    }

    if (to.name === 'mini-games-unavailable') {
      return true
    }

    try {
      await registerMiniGamesRoutes(router)
      return { path: to.fullPath, replace: true }
    } catch {
      return { path: to.fullPath, replace: true }
    }
  }

  if (to.name === 'quizzes-courses-unavailable') {
    return true
  }

  try {
    await registerQuizzesCoursesRoutes(router)
    return { path: to.fullPath, replace: true }
  } catch {
    return { path: to.fullPath, replace: true }
  }
})

export default router
