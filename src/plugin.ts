import streamDeck from '@elgato/streamdeck';
import { ReferenceButton } from './actions/reference-button';

streamDeck.actions.registerAction(new ReferenceButton());
streamDeck.connect();
