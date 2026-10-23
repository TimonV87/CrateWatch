import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'

const shared = { primary: '#0A84FF', info: '#0A84FF', love: '#FF375F' }

export default createVuetify({
  theme: {
    defaultTheme: 'system',
    themes: {
      light: {
        colors: { ...shared, background: '#FFFFFF', surface: '#F5F5F7' },
      },
      dark: {
        colors: { ...shared, background: '#121214', surface: '#1C1C1E' },
      },
    },
  },
  defaults: {
    VCard: { rounded: 'xl' },
    VBtn: { rounded: 'lg' },
    VTextField: { variant: 'solo-filled', flat: true, rounded: 'lg' },
  },
})
