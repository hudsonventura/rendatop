import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css'
import { ThemeProvider } from '@/components/theme-provider'
import { SidebarConfigProvider } from '@/contexts/sidebar-context'
import { ROUTER_BASENAME } from '@/utils/appPath'

import Login from "@/pages/Login"
import Signup from "@/pages/Signup"
import Home from "@/pages/Home"
import Logout from "@/pages/Logout"
import CalendarPage from "@/pages/CalendarPage"
import UserSettings from "@/pages/UserSettings"
import MyInvestments from "@/pages/MyInvestments"
import NotificationsPage from "@/pages/NotificationsPage"
import ResetPassword from "@/pages/ResetPassword"
import ForgotPassword from "@/pages/ForgotPassword"
import ForgotTotp from "@/pages/ForgotTotp"
import ResetTotp from "@/pages/ResetTotp"
import NotFound from "@/pages/NotFound"
import SubscriptionPage from "@/pages/SubscriptionPage"


function App() {
    return (
        <div className="font-sans antialiased" style={{ fontFamily: 'var(--font-inter)' }}>
            <ThemeProvider defaultTheme="system" storageKey="vite-ui-theme">
                <SidebarConfigProvider>
                    <Router basename={ROUTER_BASENAME}>
                        <Routes>
                            <Route path="/" element={<Login />} />
                            <Route path="/login" element={<Login />} />
                            <Route path="/signup" element={<Signup />} />
                            <Route path="/logout" element={<Logout />} />
                            <Route path="/home" element={<Home />} />
                            <Route path="/meus-investimentos" element={<MyInvestments />} />
                            <Route path="/calendar" element={<CalendarPage />} />
                            <Route path="/notifications" element={<NotificationsPage />} />
                            <Route path="/settings" element={<UserSettings />} />
                            <Route path="/forgot-password" element={<ForgotPassword />} />
                            <Route path="/reset-password" element={<ResetPassword />} />
                            <Route path="/forgot-totp" element={<ForgotTotp />} />
                            <Route path="/reset-totp" element={<ResetTotp />} />
                            <Route path="/subscription" element={<SubscriptionPage />} />
                            <Route path="*" element={<NotFound />} />
                        </Routes>
                    </Router>
                </SidebarConfigProvider>
            </ThemeProvider>
        </div>
    );
}

export default App
