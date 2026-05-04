import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

const PUBLIC_KEY_PEM = `-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAm+ld/A8HSAl8DU54M17Q
cn0P7roy2wnoogCPpJcQE4pW7t1/G6zv62+8gm1G8+cL9PpbPAwzK0RjvV3Mwo6y
HH9CJS189DnkPgNTQxVlnR4bD0hQL1GfRUG8Slcw/1bnV+kVQSjsSKnuwuRAHDJF
9dt1ENQTHekipZphpmZBFEd74OgSYRGDJyhjxD7RE9JTSNtYQtZfOy+WNvieIXu1
KV6N9MFeDUwtsGc9HywB1BOk+bGQKL0e6/5rxwy3lT4TS4UZvtZ/EhHeoAyyrC+N
/OTbtjLqVs443PddCMsnszCl75/78RqycYZx/NmlYHH0giJCaNitEw36HVhB7lsJ
+wIDAQAB
-----END PUBLIC KEY-----`

const PRIVATE_KEY_NOTE = 'private key is embedded only in backend'

export const authService = {
  getPublicKey: async () => ({ publicKey: PUBLIC_KEY_PEM }),
  login: async (email, password) => {
    const { data } = await api.post('/auth/login', { email, password })
    return data
  },
  register: async (name, email, password) => {
    const { data } = await api.post('/auth/register', { name, email, password })
    return data
  },
  getPrivateKeyNote: () => PRIVATE_KEY_NOTE
}

export default api
