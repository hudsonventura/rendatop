import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css'
import { ThemeProvider } from '@/components/theme-provider'
import { SidebarConfigProvider } from '@/contexts/sidebar-context'

import Login from "@/pages/Login"
import Signup from "@/pages/Signup"
import Home from "@/pages/Home"
import Logout from "@/pages/Logout"
import CalendarPage from "@/pages/CalendarPage"


function App() {
    return (
        <div className="font-sans antialiased" style={{ fontFamily: 'var(--font-inter)' }}>
            <ThemeProvider defaultTheme="system" storageKey="vite-ui-theme">
                <SidebarConfigProvider>
                    <Router>
                        <Routes>
                            <Route path="/" element={<Login />} />
                            <Route path="/login" element={<Login />} />
                            <Route path="/signup" element={<Signup />} />
                            <Route path="/logout" element={<Logout />} />
                            <Route path="/home" element={<Home />} />
                            <Route path="/calendar" element={<CalendarPage />} />
                        </Routes>
                    </Router>
                </SidebarConfigProvider>
            </ThemeProvider>
        </div>
    );
}

export default App
