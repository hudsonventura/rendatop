# Use the official Node.js image as the base image
FROM node:18 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy package.json and package-lock.json to the working directory
COPY package.json package-lock.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application code to the working directory
COPY . .

# Build the Vite React app
RUN npm run build

EXPOSE 5173

# Start the Vite development server and accept connections from other machines
CMD ["npm", "run", "dev", "--", "--host"]

