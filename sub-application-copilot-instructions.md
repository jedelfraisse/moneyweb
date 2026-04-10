# 🧭 Sub-Application Development Guide

> **Integration Guide for Applications in the Delfraisse.com Directory**
> Use these instructions when developing applications that may integrate with the Delfraisse.com portal and directory.

---

## 🎯 Core Philosophy: Independence First

### ✅ **Primary Principle**: Your Application Must Function Independently
- Your app should work completely without the Delfraisse.com portal
- Integration with shared services is **optional** and **enhances** functionality
- Users should be able to access and use your app directly
- The portal serves as a **directory** and **optional service provider**, not a dependency

### 🌐 Architecture Overview
- **Main Portal**: `https://www.delfraisse.com` - Directory, showcase, optional user services
- **Shared API**: `https://api.delfraisse.com` - Optional integration services
- **Your App**: `https://[YOUR_SUBDOMAIN].delfraisse.com` or `https://your-domain.com` - Your independent application
- **External Projects**: May have dedicated sections on the portal without their own websites

---

## � Development Approaches

### Option 1: Fully Independent (Recommended Starting Point)
```javascript
// Your app handles everything independently
const MyApp = {
  authentication: "Your own auth system",
  userManagement: "Your own user database", 
  functionality: "Complete business logic",
  hosting: "Independent deployment",
  discovery: "Listed in Delfraisse.com directory"
};
```

**Benefits:**
- Complete control over your application
- No external dependencies
- Faster development and deployment
- Listed in the portal directory for discoverability

### Option 2: Enhanced Integration (Optional)
```javascript
// Your app uses shared services for enhanced features
const EnhancedApp = {
  authentication: "Optional: Microsoft Entra ID integration",
  userManagement: "Optional: Shared user profiles",
  functionality: "Your complete business logic", 
  hosting: "Independent deployment",
  discovery: "Featured in portal with enhanced integration"
};
```

**Benefits:**
- Single sign-on across integrated apps
- Shared user profiles and preferences  
- Cross-application notifications
- Unified dashboard experience (optional)

---

## 🔧 Optional Integration Services

### 🔗 Available Shared API Endpoints
*Only use these if you want enhanced integration*

#### 1. **Membership Validation** (Optional)
```http
GET https://api.delfraisse.com/api/Membership/validate-access/{YOUR_APP_IDENTIFIER}
Authorization: Bearer {JWT_TOKEN}
```
- **Purpose**: Check if current user has access to enhanced features
- **Fallback**: Your app works normally without this validation
- **Response**: `{ "HasAccess": true, "GrantedAt": "2025-01-01", "ExpiresAt": null }`
- **App Identifier**: Use subdomain name (e.g., "kitchen") or agreed identifier for your domain

#### 2. **User Profile Sync** (Optional)
```http
GET https://api.delfraisse.com/api/Membership/profile
Authorization: Bearer {JWT_TOKEN}
```
- **Purpose**: Get shared user information for enhanced experience
- **Fallback**: Use your own user management system

#### 3. **Application Directory** (For Portal Integration)
```http
GET https://api.delfraisse.com/api/Applications/public
```
- **Purpose**: Get list of other applications for cross-linking
- **Use for**: "Other Apps" navigation menus

#### 4. **Cross-App Notifications** (Optional)
```http
POST https://api.delfraisse.com/api/Invitations
Authorization: Bearer {JWT_TOKEN}
Body: { "Email": "user@example.com", "ApplicationId": "YOUR_APP_ID" }
```
- **Purpose**: Send notifications across integrated applications

---

## 🏠 Hosting Options

### Option A: Subdomain Hosting
- **URL Pattern**: `https://[YOUR_SUBDOMAIN].delfraisse.com`
- **Examples**: `kitchen.delfraisse.com`, `pta.delfraisse.com`
- **Benefits**: Unified domain experience, easier CORS configuration
- **Setup**: Subdomain provided by Delfraisse.com team

### Option B: Your Own Domain
- **URL Pattern**: `https://your-domain.com`
- **Examples**: `https://myawesomeapp.com`, `https://companyname.net`
- **Benefits**: Complete brand control, independent hosting
- **Setup**: Your own domain registration and hosting

### Option C: Portal-Hosted Projects
- **Free Showcase**: `https://delfraisse.com/projects/your-project` - Project descriptions and details
- **Paid Application Hosting**: `https://your-project.delfraisse.com` - Full application hosting (pricing TBD)
- **Target Audience**: Projects without websites, startups, individual developers
- **Benefits**: No hosting setup required, managed infrastructure, integrated with portal directory
- **Revenue Model**: Free project descriptions, paid application hosting services

---

## 🎨 Portal Directory Benefits

### What the Portal Provides:
- **Discoverability**: Your app is showcased in the directory
- **Project Description**: Dedicated page describing your application (free)
- **Direct Links**: Users can find and access your app easily
- **Application Hosting**: Full application hosting services (paid, pricing TBD)
- **Technology Showcase**: Highlight your chosen tech stack and approach
- **Managed Infrastructure**: Optional hosting with no setup required

### What You Need to Provide:
- **App Information**: Name, description, technology stack
- **Direct Access URL**: Your subdomain, domain, or request for project section/hosting
- **Contact Information**: How users can get support or learn more
- **Screenshots/Demos**: Visual representation of your application
- **Hosting Preference**: Subdomain, own domain, free showcase, or paid hosting service

---

## �️ Implementation Patterns

### Pattern 1: Independent App with Portal Listing
```javascript
// Minimal integration - just listed in directory
const IndependentApp = {
  setup: () => {
    // Your standard app initialization
    initializeApp();
    
    // Optional: Add "Back to Directory" link
    addPortalNavigation();
  },
  
  addPortalNavigation: () => {
    // Simple link back to discovery portal
    const navLink = '<a href="https://www.delfraisse.com">← Browse More Apps</a>';
    document.querySelector('header').innerHTML += navLink;
  }
};
```

### Pattern 2: Enhanced Integration
```javascript
// Full integration with shared services
const EnhancedApp = {
  initialize: async () => {
    // Try enhanced integration first
    try {
      const user = await getSharedUserProfile();
      initializeWithSharedUser(user);
    } catch (error) {
      // Fallback to independent operation
      initializeIndependently();
    }
  },
  
  getSharedUserProfile: async () => {
    const token = getAuthToken();
    if (!token) throw new Error('No shared auth');
    
    const response = await fetch('https://api.delfraisse.com/api/Membership/profile', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    
    if (!response.ok) throw new Error('Shared API unavailable');
    return response.json();
  },
  
  initializeIndependently: () => {
    // Your app works normally without shared services
    console.log('Running in independent mode');
    setupLocalAuth();
    loadAppContent();
  }
};
```

---

## 🔐 Authentication Strategies

### Independent Authentication
```javascript
// Your own auth system
const IndependentAuth = {
  login: (username, password) => {
    // Your authentication logic
    return authenticateLocally(username, password);
  },
  
  getUserInfo: () => {
    // Your user management
    return getCurrentUser();
  }
};
```

### Optional Shared Authentication
```javascript
// Enhanced with Entra ID integration
const EnhancedAuth = {
  login: async () => {
    try {
      // Try shared authentication
      return await authenticateWithEntraID();
    } catch (error) {
      // Fallback to your own auth
      return showLocalLoginForm();
    }
  },
  
  authenticateWithEntraID: async () => {
    const authConfig = {
      authority: "https://login.microsoftonline.com/YOUR_TENANT_ID",
      clientId: "YOUR_APP_CLIENT_ID", 
      redirectUri: "https://your-app-domain.com/auth/callback" // or subdomain.delfraisse.com
    };
    
    return await performEntraIDAuth(authConfig);
  }
};
```

---

## 🌟 Portal Integration Levels

### Level 1: Directory Listing (Minimal)
- ✅ App listed in portal directory
- ✅ Basic description and link
- ✅ Independent operation
- ✅ No technical dependencies

### Level 2: Enhanced Discovery (Recommended)
- ✅ Detailed app showcase page
- ✅ Screenshots and feature descriptions
- ✅ Technology stack highlights  
- ✅ "Back to Directory" navigation
- ✅ Still fully independent

### Level 3: Service Integration (Advanced)
- ✅ Optional shared authentication
- ✅ Cross-app user profiles
- ✅ Unified notifications
- ✅ Dashboard integration
- ✅ Graceful fallback to independent mode

### Level 4: Portal-Hosted Services
- ✅ **Free**: Dedicated project showcase section on delfraisse.com
- ✅ **Paid**: Full application hosting with custom subdomain (pricing TBD)
- ✅ **Managed Infrastructure**: No hosting setup or maintenance required
- ✅ **Revenue Model**: Monetization opportunity for portal operations

---

## �️ Development Environment Setup

### Independent Development
```env
# Your app's own configuration
APP_NAME=your-app-name
APP_PORT=3000
DATABASE_URL=your-database
AUTH_SECRET=your-secret

# Optional portal integration
PORTAL_DIRECTORY_URL=https://www.delfraisse.com
```

### Enhanced Integration (Optional)
```env
# Shared services (optional)
AZURE_AD_TENANT_ID=your-tenant-id
AZURE_AD_CLIENT_ID=your-app-client-id
SHARED_API_URL=https://api.delfraisse.com
APP_IDENTIFIER=your-subdomain-or-agreed-identifier

# Your app's own configuration
APP_NAME=your-app-name
APP_DOMAIN=your-domain.com  # or subdomain.delfraisse.com
DATABASE_URL=your-database
```

---

## � Error Handling & Fallbacks

### Graceful Degradation Pattern
```javascript
const RobustApp = {
  initialize: async () => {
    try {
      // Attempt enhanced features
      await enableEnhancedFeatures();
      console.log('Running with enhanced integration');
    } catch (error) {
      // Fallback to core functionality
      enableCoreFeatures();
      console.log('Running in independent mode');
    }
  },
  
  enableEnhancedFeatures: async () => {
    // Try shared services
    const apiHealth = await checkSharedAPI();
    if (!apiHealth.ok) throw new Error('Shared API unavailable');
    
    await setupSharedAuth();
    await loadSharedUserData();
  },
  
  enableCoreFeatures: () => {
    // Your app's core functionality
    setupLocalAuth();
    loadLocalUserData();
    enableOfflineMode();
  }
};
```

---

## 📋 Integration Checklist

### For Directory Listing (All Apps):
- [ ] App name and description ready
- [ ] Direct access URL confirmed (subdomain, own domain, free showcase, or paid hosting)
- [ ] Technology stack documented
- [ ] Contact information provided
- [ ] Screenshots or demo prepared
- [ ] Hosting preference specified (including budget for paid hosting if applicable)

### For Enhanced Integration (Optional):
- [ ] Entra ID app registration (if using shared auth)
- [ ] Shared API integration points identified
- [ ] Fallback mechanisms implemented
- [ ] Error handling for service unavailability
- [ ] CORS configuration for your domain
- [ ] Local development environment tested

### For External Projects:
- [ ] Project description and goals defined
- [ ] Content for dedicated portal section prepared
- [ ] Contact information and links provided
- [ ] Any screenshots or documentation ready

---

## 🎯 Quick Start Guide

### Step 1: Build Your Independent App
- Develop your application to work completely standalone
- Implement your own authentication and user management
- Ensure core functionality works without external dependencies

### Step 2: Add Directory Integration
- Prepare app description and showcase materials
- Add simple "Back to Directory" navigation
- Test independent operation thoroughly

### Step 3: Consider Enhanced Integration (Optional)
- Evaluate if shared services would benefit your users
- Implement optional integration with graceful fallbacks
- Test both integrated and independent modes

### Step 4: Submit for Directory Inclusion
- Contact the portal team with your app information
- Provide all necessary showcase materials
- Choose your integration level

---

## 📞 Support & Resources

### Getting Listed in Directory
- **Contact**: Submit app information to portal team
- **Requirements**: Working application with direct access URL (subdomain, domain, or hosting service)
- **Timeline**: Apps reviewed and added regularly
- **Hosting Options**: Choose between subdomain, own domain, free showcase, or paid hosting service
- **Pricing**: Free project showcases, paid application hosting (pricing TBD)

### Technical Integration Support  
- **Shared API Documentation**: Available in main portal repository
- **Authentication Help**: Entra ID configuration assistance
- **CORS Configuration**: Portal team can whitelist your domain

### Portal-Hosted Services
- **Free Project Showcases**: Detailed description pages for projects and applications
- **Paid Application Hosting**: Full hosting services with custom subdomains (pricing TBD)
- **Managed Infrastructure**: Complete hosting solution with no technical setup required
- **Revenue Generation**: Sustainable income model for portal operations
- **Target Market**: Individual developers, startups, small projects without hosting expertise

---

> **Remember**: The goal is **independence first, enhancement second**. Your application should provide complete value to users on its own, with the portal serving as a directory for discovery and optional services for enhanced integration.
