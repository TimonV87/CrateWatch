import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import ArtistListView from '../views/ArtistListView.vue'
import ArtistSearchView from '../views/ArtistSearchView.vue'
import ArtistDetailView from '../views/ArtistDetailView.vue'
import ReleaseStateListView from '../views/ReleaseStateListView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'

const PUBLIC_PATHS = new Set(['/login', '/register'])

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: ArtistListView },
    { path: '/artists/search', component: ArtistSearchView },
    { path: '/artists/:id', component: ArtistDetailView },
    { path: '/collection', component: ReleaseStateListView, props: { state: 'OWNED', title: 'My collection' } },
    { path: '/wishlist', component: ReleaseStateListView, props: { state: 'WISHLIST', title: 'My wishlist' } },
    { path: '/login', component: LoginView },
    { path: '/register', component: RegisterView },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!PUBLIC_PATHS.has(to.path) && !auth.isAuthenticated) {
    return '/login'
  }
  return true
})

export default router
